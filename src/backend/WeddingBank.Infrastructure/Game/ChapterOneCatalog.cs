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

    public const string SecondQuestReference = "CH-I-QUEST-002";
    public const string SecondQuestTitle = "The Unconfirmed Transfer";
    public const string SecondQuestDescription = "The supporting record was transferred to departmental review on 30 October 2026. The receiving department has not confirmed physical receipt.";
    public const string SecondQuestInstructions = "Using the available personal records and the Departmental Transfer Register, identify the department that received the supporting documentation associated with the transfer.";
    public const string SecondTaskReference = "CH-I-TASK-002";
    public const string SecondTaskTitle = "Identify the Receiving Department";
    public const string SecondTaskInstructions = "Using the available personal records and the Departmental Transfer Register, identify the department that received the supporting documentation associated with the transfer.\n\nSubmit the name of the receiving department.";
    public const string SecondAnswer = "Compliance";
    public const string SecondLockedDocument = "CH-I-DOC-011";

    public static readonly ChapterOneQuest[] Quests =
    [
        new(QuestReference, QuestTitle, QuestDescription, QuestInstructions, true, null, new ChapterOneTask(TaskReference, TaskTitle, TaskInstructions, AcceptedAnswer, SuccessMessage, [LockedDocument, "CH-I-DOC-008", "CH-I-DOC-009", "CH-I-DOC-010"])),
        new(SecondQuestReference, SecondQuestTitle, SecondQuestDescription, SecondQuestInstructions, true, TaskReference, new ChapterOneTask(SecondTaskReference, SecondTaskTitle, SecondTaskInstructions, SecondAnswer, null, [SecondLockedDocument])),
    ];

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
            "WEDDINGBANK\nPrivate Banking · Account Services\n\nINTERNAL PROCESSING NOTE\n\nReference: RA-2026-1103-08\nDate: 03 November 2026\nSubject: Supporting documentation — allocation review\n\nThe supporting documentation relating to the restricted allocation has been reviewed against the current registry.\n\nThe registry indicates that the relevant documentation was received and registered under its original entry.\n\nHowever, no corresponding document could be located in the electronic case file at the time of this review.\n\nThe receipt status therefore remains recorded as acknowledged — confirmation pending.\n\nFurther verification has been requested before the internal review can be concluded.\n\nDistribution: Account Services\n\nInternal administrative record. Not intended for external distribution."),
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
            "WEDDINGBANK\nPrivate Banking · Account Services\n\nDOCUMENT RECEIPT CONFIRMATION\n\nThe relevant supporting documentation was registered on 29 October 2026.\n\nThe original reference number was not transferred to the electronic case file.\n\nDistribution: Account Holder — Amber"),
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
        new(
            "CH-I-DOC-008",
            "Dispatch Control Extract",
            "Extract",
            "JUUL",
            true,
            "WEDDINGBANK\nPrivate Banking · Records Administration\n\nDISPATCH CONTROL EXTRACT\n\nThe transfer relating to the supporting documentation was included in dispatch batch TR-1030.\n\nThe batch contains several departmental movements.\n\nThe receiving department is not identified in this extract.\n\nDistribution: Account Holder — Juul"),
        new(
            "CH-I-DOC-009",
            "Routing Classification Notice",
            "Notice",
            "AMBER",
            true,
            "WEDDINGBANK\nPrivate Banking · Records Administration\n\nROUTING CLASSIFICATION NOTICE\n\nThe supporting record was assigned routing category B before dispatch.\n\nThis classification identifies the processing route used for the transfer.\n\nIt does not independently identify the receiving department.\n\nDistribution: Account Holder — Amber"),
        new(
            "CH-I-DOC-010",
            "Departmental Transfer Register",
            "Register",
            "BOTH",
            true,
            "WEDDINGBANK\nPrivate Banking · Records Administration\n\nDEPARTMENTAL TRANSFER REGISTER\n\nReference | Routing Category | Receiving Department | Subject\n\nTR-1030-A | A | Account Services | Account review\nTR-1030-B | B | Compliance | Supporting documentation\nTR-1030-C | C | Legal Affairs | Supporting documentation\nTR-1031-B | B | Records Administration | Correspondence\n\nInternal administrative record."),
        new(
            "CH-I-DOC-011",
            "Compliance Intake Exception",
            "Record",
            "BOTH",
            false,
            "WEDDINGBANK\nPrivate Banking · Compliance\n\nCOMPLIANCE INTAKE EXCEPTION\n\nReference: CI-2026-1030-07\nSubject: Supporting Documentation\n\nThe transfer associated with reference TR-1030-B appears in the departmental intake register.\n\nThe item was recorded as electronically received.\n\nHowever, the original supporting record could not be located in the corresponding physical case file.\n\nThe intake entry therefore remains subject to verification.\n\nNo final receipt confirmation has been issued.\n\nInternal administrative record."),
    ];
}

public sealed record ChapterOneQuest(
    string Reference,
    string Title,
    string Description,
    string Instructions,
    bool Published,
    string? ReleasedAfterTask,
    ChapterOneTask Task);

public sealed record ChapterOneTask(
    string Reference,
    string Title,
    string Instructions,
    string AcceptedAnswer,
    string? SuccessMessage,
    string[] Unlocks);
