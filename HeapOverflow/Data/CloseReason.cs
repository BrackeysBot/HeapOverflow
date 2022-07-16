using System.ComponentModel;

namespace HeapOverflow.Data;

/// <summary>
///     An enumeration of reasons to close a question.
/// </summary>
public enum CloseReason
{
    [Description("The question was answered and resolved.")]
    Resolved,

    [Description("Another identical (or similar) question has been asked and answered already.")]
    Duplicate,

    [Description("The thread does not constitute a valid question (spam, or otherwise).")]
    Invalid
}
