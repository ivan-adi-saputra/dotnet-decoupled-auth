namespace AuthApp.Api.Validation;

/// <summary>
/// A small, hand-curated list of passwords that repeatedly top public breach/most-common
/// password reports (RockYou, SplashData/NordPass annual lists, etc.) — not a full
/// HaveIBeenPwned-style check. That would need either a live API call (a runtime network
/// dependency this project deliberately avoids everywhere else — see notifications.js being
/// vendored locally instead of loaded from a CDN) or bundling a multi-gigabyte dataset,
/// neither of which fits a self-contained test project. This is a deliberately scoped,
/// honest implementation of the same NIST 800-63B guidance ("compare against a list of
/// commonly-used, expected, or compromised passwords"), not a substitute for the real thing.
/// </summary>
public static class CommonPasswords
{
    private static readonly HashSet<string> Blocklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "password1", "password123", "passw0rd",
        "123456", "1234567", "12345678", "123456789", "1234567890",
        "111111", "000000", "123123", "1q2w3e4r", "1qaz2wsx", "qazwsx",
        "qwerty", "qwerty123", "qwertyuiop", "asdfghjkl", "zxcvbnm",
        "iloveyou", "iloveyou1", "iloveyou2",
        "abc123", "admin", "admin123", "login", "welcome", "welcome1",
        "letmein", "letmein1", "letmeinnow", "opensesame",
        "monkey", "monkey123", "dragon", "dragon123", "master", "master123",
        "superman", "superman123", "batman", "ironman", "spiderman", "wonderwoman",
        "trustno1", "trustno11", "whatever", "whatever1", "freedom", "freedom1",
        "sunshine", "sunshine1", "princess", "princess1",
        "football", "football1", "baseball", "baseball1", "basketball",
        "soccer", "hockey", "golfer",
        "michael", "jennifer", "jessica", "jasmine", "chelsea", "amanda", "ashley",
        "daniel", "hannah", "joshua", "maggie", "mickey", "tigger", "mercedes",
        "andrew", "charlie", "andrea", "midnight", "robert", "thomas", "george",
        "jordan23", "harley", "ranger", "buster", "killer", "hunter2",
        "computer", "internet", "matrix", "ninja", "dolphin", "pokemon",
        "minecraft", "fortnite", "starwars",
        "cheese", "cookie", "banana", "purple", "orange", "ginger", "peanut",
        "biteme", "blahblah", "changeme", "changeme123", "temp123", "temppass",
        "guest", "guest123", "test123", "test1234", "demo123",
        "newpassword", "mypassword", "mynewpassword", "p@ssw0rd", "p@ssword"
    };

    public static bool IsCommon(string password) => Blocklist.Contains(password);
}
