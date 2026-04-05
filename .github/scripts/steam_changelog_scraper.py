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


def parse_timestamp(date_text, raw_html, raw_search_start):
    raw_index = raw_html.find(date_text, raw_search_start)
    if raw_index != -1:
        snippet = raw_html[max(0, raw_index - 600):raw_index + 600]
        attr_match = re.search(r'data-(?:timestamp|rtime(?:_updated)?|time_updated)="(\d+)"', snippet)
        if attr_match:
            return int(attr_match.group(1)), raw_index + len(date_text)

    # Handle date format without year (e.g., "21 Mar @ 1:19pm")
    # Add current year; if parsing fails in the future, adjust to previous year
    from datetime import datetime as dt_module
    current_year = dt_module.now(timezone.utc).year
    try:
        parsed = datetime.strptime(f"{date_text} {current_year}", "%d %b @ %I:%M%p %Y")
    except ValueError:
        # If current year doesn't work, try previous year
        try:
            parsed = datetime.strptime(f"{date_text} {current_year - 1}", "%d %b @ %I:%M%p %Y")
        except ValueError:
            # Fallback: try the old format just in case
            try:
                parsed = datetime.strptime(date_text, "%d %b, %Y @ %I:%M%p")
            except ValueError:
                # If all else fails, return a safe fallback
                parsed = datetime.now(timezone.utc)
    
    parsed = parsed.replace(tzinfo=timezone.utc)
    return int(parsed.timestamp()), raw_index + len(date_text) if raw_index != -1 else raw_search_start


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

    header_re = re.compile(
        r"Update:\s+(?P<date>\d{1,2}\s+[A-Za-z]{3}\s+@\s+\d{1,2}:\d{2}[ap]m)\s+by\s+(?P<author>.*?)(?:\n|$)"
    )

    entries = []
    page_number = 1

    try:
        while page_number <= 50:
            raw_html = fetch_page(base_url, headers, page_number)
            page_text = html_to_text(raw_html)
            matches = list(header_re.finditer(page_text))

            if not matches:
                break

            raw_search_start = 0
            for index, match in enumerate(matches):
                next_start = matches[index + 1].start() if index + 1 < len(matches) else len(page_text)
                body = page_text[match.end():next_start].strip()

                footer_split = re.split(
                    r"\n(?:Showing\s+\d+-\d+\s+of\s+\d+\s+entries|Additional Links)\b",
                    body,
                    maxsplit=1,
                )
                body = footer_split[0].strip()

                entry_ts, raw_search_start = parse_timestamp(match.group("date"), raw_html, raw_search_start)
                if entry_ts <= previous_ts:
                    raise StopIteration

                if not body:
                    body = "No changelog details were provided for this update."

                entry_date = datetime.fromtimestamp(entry_ts, tz=timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
                entries.append(f"### {entry_date}\n\n{body}")

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