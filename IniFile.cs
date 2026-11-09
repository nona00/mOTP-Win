// Small INI reader/writer replacing the IniFileVb.vb dependency referenced by
// the original Visual Basic .NET application.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace mOTPWin;

internal sealed class IniFile
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections =
        new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<string> SectionNames => _sections.Keys.Where(name => name.Length > 0);

    public void Load(string filename)
    {
        _sections.Clear();
        string currentSection = string.Empty;
        GetOrCreateSection(currentSection);

        foreach (string originalLine in File.ReadLines(filename, Encoding.UTF8))
        {
            string line = originalLine.Trim();
            if (line.Length == 0 || line.StartsWith(";", StringComparison.Ordinal) ||
                line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("[", StringComparison.Ordinal) &&
                line.EndsWith("]", StringComparison.Ordinal))
            {
                currentSection = line.Substring(1, line.Length - 2).Trim();
                GetOrCreateSection(currentSection);
                continue;
            }

            int separatorIndex = line.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            string key = line.Substring(0, separatorIndex).Trim();
            if (key.Length == 0)
            {
                continue;
            }

            string value = line.Substring(separatorIndex + 1).Trim();
            GetOrCreateSection(currentSection)[key] = value;
        }
    }

    public string GetKeyValue(string sectionName, string key)
    {
        return _sections.TryGetValue(sectionName, out Dictionary<string, string>? section) &&
               section.TryGetValue(key, out string? value)
            ? value
            : string.Empty;
    }

    public void SetKeyValue(string sectionName, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentException("A profile name must not be empty.", nameof(sectionName));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("An INI key must not be empty.", nameof(key));
        }

        GetOrCreateSection(sectionName)[key] = value;
    }

    public void RemoveSection(string sectionName)
    {
        _sections.Remove(sectionName);
    }

    public void Save(string filename)
    {
        string? directory = Path.GetDirectoryName(filename);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var output = new StringBuilder();
        foreach (KeyValuePair<string, Dictionary<string, string>> section in _sections)
        {
            if (section.Key.Length > 0)
            {
                output.Append('[').Append(section.Key).AppendLine("]");
            }

            foreach (KeyValuePair<string, string> entry in section.Value)
            {
                output.Append(entry.Key).Append('=').AppendLine(entry.Value);
            }

            output.AppendLine();
        }

        File.WriteAllText(filename, output.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private Dictionary<string, string> GetOrCreateSection(string sectionName)
    {
        if (!_sections.TryGetValue(sectionName, out Dictionary<string, string>? section))
        {
            section = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _sections.Add(sectionName, section);
        }

        return section;
    }
}
