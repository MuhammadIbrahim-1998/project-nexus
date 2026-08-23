using Nexus.Application.Common.Models;
using System.Text.RegularExpressions;

namespace Nexus.Infrastructure.Agents.Discovery;

public static class JobRelevanceFilter
{
    private static readonly string[] SoftwareRoleKeywords = { "developer", "engineer", "programmer", "architect", "coder", "software", "backend", "back end", "back-end", "frontend", "front end", "front-end", "full stack", "full-stack", "fullstack", "web developer", "web dev", "sdet", "qa" };

    private static readonly string[] CoreBackendKeywords = { ".net", ".net core", ".net framework", "asp.net", "asp.net core", "asp.net mvc", "c#", "csharp", "web api", "winforms", "entity framework", "ef core" };

    private static readonly string[] ArchitectureKeywords = { "cqrs", "mediatr", "clean architecture", "solid", "unit of work", "repository pattern", "microservices" };

    private static readonly string[] DatabaseKeywords = { "sql server", "mysql", "postgresql", "postgres", "mongodb", "oracle database", "oracle sql", "tsql", "t-sql" };

    private static readonly string[] CloudDevOpsKeywords = { "azure", "aws", "docker", "kubernetes", "ci/cd", "github actions", "devops", "terraform", "jenkins", "pipelines" };

    private static readonly string[] FrontendKeywords = { "react", "react.js", "next.js", "nextjs", "vue", "vue.js", "angular", "typescript", "tailwind" };

    private static readonly string[] RegionRestrictionPhrases = { "eu only", "europe only", "european only", "only eu", "only europe", "latin america only", "latin america", "latam", "latam only", "asia only", "apac only", "emea only", "only in europe", "restricted to europe", "restricted to latam" };

    private static readonly string[] OnsiteIndicatorPhrases =
    {
        "onsite", "on-site", "100% onsite", "100% on-site", "100% in office",
        "hybrid schedule", "hybrid work", "hybrid model", "hybrid environment", "hybrid setup", "role is hybrid",
        "days onsite", "days on-site", "telework", "no chance of 100% remote", "not a remote", "not fully remote", "remote not possible",
        "metro area", "local candidates", "local to", "onsite interview", "on-site interview", "in-person interview",
        "security clearance", "top secret", "ts/sci", "polygraph",
        "u.s. citizenship", "us citizenship", "citizenship required",
        "eligible to work in the united states", "authorized to work in the united states"
    };

    private static readonly string[] MandatoryLanguagePhrases = { "fluent in german", "fluent german", "german language", "german speaking", "deutschkenntnisse", "native german", "german required", "fluent in french", "fluent french", "french language", "french speaking", "native french", "french required", "fluent in spanish", "fluent spanish", "spanish language", "spanish speaking", "native spanish", "spanish required", "fluent in dutch", "fluent dutch", "dutch language", "dutch speaking", "native dutch", "dutch required", "fluent in italian", "fluent italian", "italian language", "italian speaking", "native italian", "italian required", "fluent in portuguese", "fluent portuguese", "portuguese language", "portuguese speaking", "native portuguese", "portuguese required", "fluent in japanese", "fluent japanese", "japanese language", "japanese speaking", "native japanese", "japanese required", "fluent in chinese", "fluent chinese", "chinese language", "chinese speaking", "native chinese", "mandarin required", "mandarin speaking", "fluent in korean", "fluent korean", "korean language", "korean speaking", "native korean", "korean required", "fluent in swedish", "fluent swedish", "swedish language", "swedish speaking", "native swedish", "swedish required", "fluent in danish", "fluent danish", "danish language", "danish speaking", "native danish", "danish required", "fluent in norwegian", "fluent norwegian", "norwegian language", "norwegian speaking", "native norwegian", "norwegian required", "fluent in polish", "fluent polish", "polish language", "polish speaking", "native polish", "polish required", "fluent in russian", "fluent russian", "russian language", "russian speaking", "native russian", "russian required", "fluent in arabic", "fluent arabic", "arabic language", "arabic speaking", "native arabic", "arabic required", "fluent in hindi", "fluent hindi", "hindi language", "hindi speaking", "native hindi", "hindi required", "language requirement", "language required" };

    private static readonly Regex[] CulturalLanguagePatterns =
    {
        new Regex(@"\b(?:fluent|native|proficient)\s+(?:in\s+)?(?:german|french|spanish|italian|dutch|portuguese|japanese|mandarin|korean|swedish|danish|norwegian|polish|russian|arabic|hindi|turkish|greek|czech|romanian|hungarian|finnish)\b", RegexOptions.IgnoreCase),
        new Regex(@"\b(?:german|french|spanish|italian|dutch|portuguese|japanese|mandarin|korean|swedish|danish|norwegian|polish|russian|arabic|hindi|turkish|greek|czech|romanian|hungarian|finnish)\s+(?:language\s+)?(?:skills?|kenntnisse|speaking|required|mandatory)\b", RegexOptions.IgnoreCase),
        new Regex(@"\b(?:deutschkenntnisse|parlez-vous\s+français|parlez\s+vous\s+francais|sprechen\s+sie|hablas\s+español|parli\s+italiano)\b", RegexOptions.IgnoreCase)
    };

    private static readonly string[] Countries = { "usa", "u\\.s\\.a", "united states", "america", "canada", "mexico", "united kingdom", "britain", "england", "scotland", "wales", "ireland", "germany", "france", "spain", "italy", "netherlands", "belgium", "switzerland", "austria", "poland", "sweden", "norway", "denmark", "finland", "portugal", "greece", "czech republic", "romania", "hungary", "bulgaria", "croatia", "serbia", "ukraine", "russia", "estonia", "latvia", "lithuania", "slovakia", "slovenia", "iceland", "luxembourg", "malta", "india", "china", "japan", "south korea", "singapore", "malaysia", "indonesia", "thailand", "vietnam", "philippines", "bangladesh", "sri lanka", "nepal", "australia", "new zealand", "israel", "turkey", "united arab emirates", "uae", "saudi arabia", "qatar", "kuwait", "oman", "bahrain", "jordan", "lebanon", "nigeria", "kenya", "south africa", "egypt", "ghana", "morocco", "tunisia", "ethiopia", "tanzania", "uganda", "rwanda", "cameroon", "brazil", "argentina", "chile", "colombia", "peru", "uruguay", "ecuador", "costa rica", "panama", "venezuela", "bolivia", "paraguay", "jamaica", "trinidad", "dominican republic", "puerto rico" };

    private static readonly string[] Demonyms = { "american", "canadian", "mexican", "british", "english", "scottish", "welsh", "irish", "german", "french", "spanish", "italian", "dutch", "belgian", "swiss", "austrian", "polish", "swedish", "norwegian", "danish", "finnish", "portuguese", "greek", "czech", "romanian", "hungarian", "bulgarian", "croatian", "serbian", "ukrainian", "russian", "estonian", "latvian", "lithuanian", "slovak", "slovenian", "icelandic", "indian", "chinese", "japanese", "korean", "singaporean", "malaysian", "indonesian", "thai", "vietnamese", "filipino", "bangladeshi", "sri lankan", "nepali", "australian", "new zealander", "israeli", "turkish", "saudi", "omani", "nigerian", "kenyan", "south african", "egyptian", "ghanaian", "moroccan", "tunisian", "ethiopian", "brazilian", "argentine", "argentinian", "chilean", "colombian", "peruvian", "uruguayan", "ecuadorian", "costa rican", "panamanian", "venezuelan" };

    private static readonly Regex[] SingleCountryRestrictionPatterns = BuildSingleCountryRestrictionPatterns();

    private static readonly Regex[] StandaloneAbbreviationPatterns =
    {
        new Regex(@"^\s*(?:us|usa|u\.s\.a?|uk|u\.k\.)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^\s*remote\s*(?:\(\s*(?:the\s+)?(?:us|usa|u\.s\.a?|uk|u\.k\.)\s*\)|[-–]\s*(?:the\s+)?(?:us|usa|u\.s\.a?|uk|u\.k\.))\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    private static Regex[] BuildSingleCountryRestrictionPatterns()
    {
        var countryAlt = string.Join("|", Countries);
        var demonymAlt = string.Join("|", Demonyms);
        const RegexOptions opts = RegexOptions.IgnoreCase | RegexOptions.Compiled;

        return new[]
        {
            new Regex($@"\b(?:must\s+be\s+|be\s+)?(?:based|located|resident|residing|living)\s+(?:in|within|inside)\s+(?:the\s+)?(?:{countryAlt})\b", opts),
            new Regex($@"\b(?:must\s+)?(?:live|reside|work)\s+(?:in|from|within)\s+(?:the\s+)?(?:{countryAlt})\b", opts),
            new Regex($@"\b(?:authorized|legally\s+authorized|eligible|permitted)\s+to\s+work\s+in\s+(?:the\s+)?(?:{countryAlt})\b", opts),
            new Regex($@"\b(?:work\s+authorization|right\s+to\s+work|visa\s+sponsorship)\s+(?:in|for)\s+(?:the\s+)?(?:{countryAlt})\b", opts),
            new Regex($@"\b(?:only|exclusively|restricted\s+to|limited\s+to|open\s+to)\s+(?:the\s+)?(?:{countryAlt})\b", opts),
            new Regex($@"\b(?:{countryAlt})\s+(?:only|citizens?|residents?|nationals?|applicants?|candidates?)\b", opts),
            new Regex($@"\b(?:{countryAlt})\s*[-–]\s*(?:based|only)\b", opts),
            new Regex($@"\b(?:{demonymAlt})\s+(?:based|only|citizens?|residents?|nationals?)\b", opts),
            new Regex($@"\b(?:candidates?\s+must\s+be\s+(?:based|located)|you\s+must\s+be\s+based|must\s+be\s+based)\s+in\s+(?:the\s+)?(?:{countryAlt})\b", opts),
            new Regex($@"\((?:\s*the\s+)?(?:{countryAlt})\s*\)", opts),
            new Regex(@"\b(?:US|USA|U\.S\.|U\.S\.A\.|UK|U\.K\.)(?:\s*[-–]\s*|\s+)(?:based|only|citizens?|residents?|nationals?)\b", opts),
            new Regex(@"\b(?:in|within)\s+(?:the\s+)?(?:US|USA|U\.S\.|U\.S\.A\.|UK|U\.K\.)\b", opts),
            new Regex(@"\bremote\s*\(\s*(?:the\s+)?(?:US|USA|U\.S\.|U\.S\.A\.|UK|U\.K\.)\s*\)", opts)
        };
    }

    public static bool IsRelevant(DiscoveredJob job)
    {
        if (job is null) return false;
        if (!job.IsRemote) return false;
        return IsRelevant(job.Title, job.Description, job.Location);
    }

    public static bool IsRelevant(string? title, string? description, string? location = null)
    {
        var titleLower = (title ?? string.Empty).ToLowerInvariant();
        var haystack = $"{titleLower} {description ?? string.Empty} {location ?? string.Empty}".ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(haystack)) return false;

        if (HasSingleCountryRestriction(haystack)) return false;
        if (HasRegionRestriction(haystack)) return false;
        if (IsStandaloneCountryAbbreviation(location)) return false;
        if (HasNonGlobalLocation(location)) return false;
        if (HasOnsiteOrHybridIndicator(haystack)) return false;

        if (HasMandatoryNonEnglishLanguageRequirement(haystack)) return false;

        if (!ContainsAny(titleLower, SoftwareRoleKeywords)) return false;

        return ContainsAny(haystack, CoreBackendKeywords)
            || ContainsAny(haystack, ArchitectureKeywords)
            || ContainsAny(haystack, DatabaseKeywords)
            || ContainsAny(haystack, CloudDevOpsKeywords)
            || ContainsAny(haystack, FrontendKeywords);
    }

    public static string? GetMatchedGroup(string? title, string? description)
    {
        var titleLower = (title ?? string.Empty).ToLowerInvariant();
        var haystack = $"{titleLower} {description ?? string.Empty}".ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(haystack)) return null;

        if (ContainsAny(haystack, CoreBackendKeywords)) return "CoreBackend";
        if (ContainsAny(haystack, ArchitectureKeywords)) return "Architecture";
        if (ContainsAny(haystack, DatabaseKeywords)) return "Database";
        if (ContainsAny(haystack, CloudDevOpsKeywords)) return "CloudDevOps";
        if (ContainsAny(haystack, FrontendKeywords)) return "Frontend";
        return null;
    }

    private static bool HasSingleCountryRestriction(string haystack)
        => SingleCountryRestrictionPatterns.Any(p => p.IsMatch(haystack));

    private static bool HasRegionRestriction(string haystack)
        => ContainsAny(haystack, RegionRestrictionPhrases);

    private static bool HasMandatoryNonEnglishLanguageRequirement(string haystack)
    {
        if (ContainsAny(haystack, MandatoryLanguagePhrases)) return true;
        foreach (var pattern in CulturalLanguagePatterns)
        {
            if (pattern.IsMatch(haystack)) return true;
        }
        return false;
    }

    private static bool IsStandaloneCountryAbbreviation(string? location)
        => !string.IsNullOrWhiteSpace(location) && StandaloneAbbreviationPatterns.Any(p => p.IsMatch(location));

    private static bool HasNonGlobalLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location)) return false;
        var loc = location.Trim().ToLowerInvariant();
        if (loc.StartsWith("remote", StringComparison.Ordinal)) return false;
        if (loc.Contains("anywhere", StringComparison.Ordinal)) return false;
        if (loc.Contains("worldwide", StringComparison.Ordinal)) return false;
        if (loc.Contains("global", StringComparison.Ordinal)) return false;
        if (loc.Contains("international", StringComparison.Ordinal)) return false;
        return true;
    }

    private static bool HasOnsiteOrHybridIndicator(string haystack)
        => ContainsAny(haystack, OnsiteIndicatorPhrases);

    private static bool ContainsAny(string haystack, string[] keywords)
        => keywords.Any(k => haystack.Contains(k, StringComparison.OrdinalIgnoreCase));
}
