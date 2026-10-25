namespace Momentum.Content
{
    public sealed record BehavioralQuestionDef(string Id, string Question, string Theme, string Tip);

    /// <summary>
    /// The HR/behavioural questions that come up again and again. Each maps to a theme so one well-prepared
    /// STAR story can cover several questions.
    /// </summary>
    public static class BehavioralCatalog
    {
        public static readonly IReadOnlyList<string> Themes = new[]
        {
            "Challenge", "Conflict", "Failure", "Leadership", "Teamwork", "Deadline", "Learning", "Initiative", "Feedback", "Impact"
        };

        public static readonly IReadOnlyList<BehavioralQuestionDef> Questions = new List<BehavioralQuestionDef>
        {
            new("b01", "Tell me about yourself.", "Pitch", "90 seconds: present (who you are, what you build), past (1–2 proof points), future (why this role). Use the pitch builder."),
            new("b02", "Why do you want to join our company?", "Pitch", "Name something specific — a product, engineering blog post or value — and connect it to what you've already done."),
            new("b03", "Walk me through your favourite project.", "Impact", "Problem → your design decisions → one hard trade-off → measurable result. Be ready to go two levels deep on any part."),
            new("b04", "Describe the most challenging technical problem you've solved.", "Challenge", "Show how you debugged: hypotheses, what you measured, what you ruled out. End with what you'd do differently."),
            new("b05", "Tell me about a time you had a conflict with a teammate.", "Conflict", "Focus on understanding their view, the data you used to decide, and the relationship afterwards. Never make the other person the villain."),
            new("b06", "Tell me about a time you failed.", "Failure", "Pick a real failure that you owned. Spend most of the answer on what you learned and changed afterwards."),
            new("b07", "Describe a time you showed leadership without authority.", "Leadership", "Show influence: you aligned people, unblocked others or set direction — titles don't matter."),
            new("b08", "Tell me about a time you worked under a tight deadline.", "Deadline", "Explain how you prioritised and what you consciously cut, and how you communicated the risk early."),
            new("b09", "Give an example of a time you learned something new quickly.", "Learning", "Show your learning method (docs, small experiments, asking experts) and how fast you became productive."),
            new("b10", "Tell me about something you did beyond your assigned work.", "Initiative", "Ownership: you noticed a gap, acted without being asked, and it made a measurable difference."),
            new("b11", "Describe a time you received critical feedback.", "Feedback", "Show you listened without being defensive, acted on it, and followed up."),
            new("b12", "Tell me about a successful team project and your role in it.", "Teamwork", "Be precise about what YOU did — use 'I' for your actions and 'we' only for the team outcome."),
            new("b13", "Tell me about a time you disagreed with a decision.", "Conflict", "Disagree and commit: you raised concerns with data, then fully supported the final call."),
            new("b14", "Describe a time you had to make a decision with incomplete information.", "Challenge", "Show how you reduced uncertainty quickly and made the decision reversible where possible."),
            new("b15", "What is your greatest strength?", "Pitch", "One strength, one concrete example that proves it, and why it matters for this role."),
            new("b16", "What is your biggest weakness?", "Feedback", "A real, non-critical weakness plus the system you're using to improve it — with evidence it's working."),
            new("b17", "Where do you see yourself in five years?", "Pitch", "Growth in depth and ownership; show the role is a genuine step in that direction."),
            new("b18", "Tell me about a time you made a mistake that affected others.", "Failure", "Own it, describe how you fixed the impact fast, and what process change prevents it now."),
            new("b19", "Tell me about a time you helped a struggling teammate.", "Teamwork", "Empathy plus action: how you noticed, what you did, and how they improved."),
            new("b20", "What is the impact you're most proud of?", "Impact", "Quantify: users, latency, money, time saved, grades — numbers make it memorable."),
        };
    }
}
