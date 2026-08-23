#!/usr/bin/env python3

import html
import re
import sys
import urllib.error
import urllib.request
from datetime import datetime, timezone
from html.parser import HTMLParser


class TextExtractor(HTMLParser):
    def __init__(self):
        super().__init__()
        self.parts = []
        self.skip_depth = 0

    def handle_starttag(self, tag, attrs):
        if tag in {"script", "style"}:
            self.skip_depth += 1
            return

        if self.skip_depth > 0:
            return

        if tag == "br":
            self.parts.append("\n")
        elif tag == "li":
            self.parts.append("\n- ")
        elif tag in {"p", "div", "tr", "section", "article", "header", "ul", "ol", "h1", "h2", "h3", "h4", "h5", "h6"}:
            self.parts.append("\n")
        elif tag in {"td", "th"}:
            self.parts.append(" ")

    def handle_endtag(self, tag):
        if tag in {"script", "style"}:
            self.skip_depth = max(0, self.skip_depth - 1)
            return

        if self.skip_depth > 0:
            return

        if tag in {"p", "div", "tr", "section", "article", "header", "ul", "ol", "h1", "h2", "h3", "h4", "h5", "h6"}:
            self.parts.append("\n")

    def handle_data(self, data):
        if self.skip_depth == 0:
            self.parts.append(data)

    def text(self):
        return html.unescape("".join(self.parts))


def fetch_page(base_url, headers, page_number):
    request = urllib.request.Request(
        f"{base_url}?p={page_number}",
        headers=headers,
    )
    with urllib.request.urlopen(request, timeout=30) as response:
        return response.read().decode("utf-8", "replace")


def html_to_text(fragment):
    parser = TextExtractor()
    parser.feed(fragment)
    parser.close()
    text = parser.text()
    text = text.replace("\r\n", "\n").replace("\r", "\n")
    text = re.sub(r"(?i)\[/?(?:b|i|u|s|quote|code|list|olist|h\d)\]", "", text)
    text = re.sub(r"(?is)\[url=([^\]]+)\](.*?)\[/url\]", r"\2 (\1)", text)
    text = re.sub(r"(?is)\[img\].*?\[/img\]", "", text)
    text = re.sub(r"[ \t]+\n", "\n", text)
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def extract_entries_from_page(raw_html, previous_ts):
    entries = []
    sections = re.split(
        r'<div class="detailBox workshopAnnouncement noFooter changeLogCtn">',
        raw_html,
        flags=re.IGNORECASE,
    )

    for section in sections[1:]:
        timestamp_match = re.search(r'<p id="(?P<timestamp>\d+)">', section, flags=re.IGNORECASE)
        if not timestamp_match:
            continue

        entry_ts = int(timestamp_match.group("timestamp"))
        if entry_ts <= previous_ts:
            break

        body_match = re.search(
            r'<p id="\d+">(?P<body>.*?)</p>',
            section,
            flags=re.IGNORECASE | re.DOTALL,
        )
        body_html = body_match.group("body") if body_match else ""
        body = html_to_text(body_html)

        if not body:
            body = "No changelog details were provided for this update."

        entry_date = datetime.fromtimestamp(entry_ts, tz=timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
        entries.append(f"### {entry_date}\n\n{body}")

    return entries


def main():
    if len(sys.argv) != 3:
        print("Usage: steam_changelog_scraper.py <workshop_id> <previous_ts>", file=sys.stderr)
        return 1

    workshop_id = sys.argv[1]
    previous_ts = int(sys.argv[2])

    base_url = f"https://steamcommunity.com/sharedfiles/filedetails/changelog/{workshop_id}"
    headers = {
        "User-Agent": (
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 "
            "(KHTML, like Gecko) Chrome/122.0 Safari/537.36"
        ),
        "Accept-Language": "en-US,en;q=0.9",
    }

    entries = []
    page_number = 1

    try:
        while page_number <= 50:
            raw_html = fetch_page(base_url, headers, page_number)
            page_entries = extract_entries_from_page(raw_html, previous_ts)

            if not page_entries:
                break

            entries.extend(page_entries)
            page_number += 1
    except StopIteration:
        pass
    except (urllib.error.URLError, TimeoutError, ValueError, OSError):
        entries = []

    if entries:
        print("\n\n".join(entries))
    else:
        print("No changelog entries were found on Steam since the previous saved timestamp.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())