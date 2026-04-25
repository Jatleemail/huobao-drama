namespace WuShuangDrama.Agents;

public static class Skills
{
    public static string LoadAgentSkills(string agentType)
    {
        var skillPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "skills", agentType, "SKILL.md");
        if (File.Exists(skillPath))
        {
            return File.ReadAllText(skillPath);
        }
        return "";
    }

    public static string LoadAllSkills()
    {
        var skillsDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "skills");
        if (!Directory.Exists(skillsDir))
        {
            return "";
        }

        var parts = new List<string>();
        foreach (var dir in Directory.GetDirectories(skillsDir))
        {
            var skillFile = Path.Combine(dir, "SKILL.md");
            if (File.Exists(skillFile))
            {
                var agentType = Path.GetFileName(dir);
                parts.Add($"=== {agentType} ===\n{File.ReadAllText(skillFile)}");
            }
        }
        return string.Join("\n\n", parts);
    }
}
