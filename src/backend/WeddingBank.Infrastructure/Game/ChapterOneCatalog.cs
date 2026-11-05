namespace WeddingBank.Infrastructure.Game;

public sealed record ChapterOneDocument(
    string Reference,
    string Title,
    string Category,
    string Audience,
    bool Published,
    string Content);

public static class ChapterOneCatalog
{
    public const string CaseReference = "CH-I-001";
    public const string CaseTitle = "The Account";
    public const string CaseSummary = "An administrative review has placed an unusual allocation of funds under restriction. Initial records appear incomplete, and further verification is required.";
    public const string CaseIntroduction = "The account holders have received notification that their allocated funds remain unavailable pending an internal review.";

    public const string QuestReference = "CH-I-QUEST-001";
    public const string QuestTitle = "The Missing Record";
    public const string QuestDescription = "An inconsistency has been identified in the records relating to the restricted allocation. Further examination of the October closing register may clarify where the missing supporting documentation was recorded.";
    public const string QuestInstructions = "Use the information available in your personal account documents and the October 2026 Closing Register to identify the correct registry entry.";

    public const string TaskReference = "CH-I-TASK-001";
    public const string TaskTitle = "Identify the Registry Entry";
    public const string TaskInstructions = "Submit the reference number of the registry entry that matches the department, date and subject identified in the available records.";
    public const string AcceptedAnswer = "REG-1026-17";
    public const string SuccessMessage = "The registry entry has been identified. The associated record is now available for examination.";
    public const string LockedDocument = "CH-I-DOC-007";

    public static readonly ChapterOneDocument[] Documents =
    [
        new(
            "CH-I-DOC-001",
            "Account Statement — 04 November 2026",
            "Statement",
            "BOTH",
            true,
            "Account holder: The account holders\nAccount status: Restricted\nAllocated funds: EUR 250.00\nAvailable funds: EUR 0.00\nRestricted funds: EUR 250.00\n\nFunds temporarily unavailable.\n\nThis statement is provided for informational purposes. The restricted allocation remains subject to internal review."),
        new(
            "CH-I-DOC-002",
            "Official Account Notice",
            "Notice",
            "BOTH",
            true,
            "WEDDINGBANK\nPrivate Banking · Account Services\n\nOFFICIAL ACCOUNT NOTICE\n\nReference: AS-2026-1104-02\nDate: 04 November 2026\nSubject: Temporary Restriction of Allocated Funds\n\nDear Account Holder,\n\nWe hereby confirm that the amount of EUR 250.00 allocated to your private account is currently subject to an administrative restriction.\n\nThe restriction was initiated following an internal review concerning the allocation and its supporting records.\n\nAt this time, the review remains open. The relevant departments have been instructed to verify the available documentation before any further action can be taken.\n\nUntil this review has been concluded, the allocated funds will remain unavailable.\n\nPlease retain this notice for your records. No further action is required from you at this stage.\n\nYours faithfully,\n\nAccount Services\nWeddingbank\n\nThis document is intended solely for the named account holder."),
        new(
            "CH-I-DOC-003",
            "Internal Processing Note",
            "Note",
            "BOTH",
            true,
            "WEDDINGBANK\nPrivate Banking · Records Administration\n\nINTERNAL PROCESSING NOTE\n\nReference: RA-2026-1103-08\nDate: 03 November 2026\nSubject: Supporting documentation — allocation review\n\nThe supporting documentation relating to the restricted allocation has been reviewed against the current registry.\n\nThe registry indicates that the relevant documentation was received and registered on 29 October 2026.\n\nHowever, no corresponding document could be located in the electronic case file at the time of this review.\n\nThe receipt status therefore remains recorded as acknowledged — confirmation pending.\n\nFurther verification has been requested before the internal review can be concluded.\n\nDistribution: Account Services\nPrepared by: Records Administration\n\nInternal administrative record. Not intended for external distribution."),
        new(
            "CH-I-DOC-004",
            "Registry Extract — Internal Reference",
            "Extract",
            "JUUL",
            true,
            "WEDDINGBANK\nPrivate Banking · Account Services\n\nREGISTRY EXTRACT — INTERNAL REFERENCE\n\nThe original receipt was processed by Records Administration.\n\nThe corresponding registry entry was filed separately from the supporting documentation.\n\nPlease refer to the October 2026 closing register.\n\nDistribution: Account Holder — Juul"),
        new(
            "CH-I-DOC-005",
            "Document Receipt Confirmation",
            "Confirmation",
            "AMBER",
            true,
            "WEDDINGBANK\nPrivate Banking · Records Administration\n\nDOCUMENT RECEIPT CONFIRMATION\n\nThe relevant supporting documentation was registered on 29 October 2026.\n\nThe original reference number was not transferred to the electronic case file.\n\nDistribution: Account Holder — Amber"),
        new(
            "CH-I-DOC-006",
            "October 2026 Closing Register",
            "Register",
            "BOTH",
            true,
            "Reference | Department | Date | Subject\nREG-1026-14 | Account Services | 28/10/2026 | Account review\nREG-1026-17 | Records Administration | 29/10/2026 | Supporting documentation\nREG-1026-19 | Compliance | 29/10/2026 | Identity verification\nREG-1026-21 | Records Administration | 30/10/2026 | Correspondence"),
        new(
            "CH-I-DOC-007",
            "Supporting Documentation — Registry Record",
            "Record",
            "BOTH",
            false,
            "WEDDINGBANK\nPrivate Banking · Records Administration\n\nREGISTRY RECORD\n\nReference: REG-1026-17\nSubject: Supporting Documentation\n\nThe original supporting record was transferred to departmental review on 30 October 2026.\n\nThe receiving department has not yet confirmed physical receipt.\n\nFurther verification remains pending.\n\nInternal administrative record."),
    ];
}
