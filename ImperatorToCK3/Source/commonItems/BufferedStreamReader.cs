using System.IO;
using System.Reflection;

/// <summary>
/// Contains extension methods for this namespace.
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Gets the current read position of the StreamReader.
    /// </summary>
    /// <param name="streamReader">The StreamReader object to get the position for.</param>
    /// <returns>Current read position in the StreamReader.</returns>
    public static int GetPosition(this StreamReader streamReader)
    {
        // Based on code shared on www.daniweb.com by user mfm24(Matt).
        int charpos = (int)streamReader.GetType().InvokeMember(
            "charPos",
            invokeAttr: BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField,
            null,
            streamReader,
            null);
        int charlen = (int)streamReader.GetType().InvokeMember(
            "charLen",
            invokeAttr: BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField,
            null,
            streamReader,
            null);
        return (int)streamReader.BaseStream.Position - charlen + charpos;
    }

    /// <summary>
    /// Sets the current read position of the StreamReader.
    /// </summary>
    /// <param name="streamReader">The StreamReader object to get the position for.</param>
    /// <param name="position">The position to move to in the file, starting from the beginning.</param>
    public static void SetPosition(this StreamReader streamReader, long position)
    {
        streamReader.BaseStream.Seek(position, SeekOrigin.Begin);
        streamReader.DiscardBufferedData();
    }
}