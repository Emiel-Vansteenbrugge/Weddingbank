using WeddingBank.Domain.Game;

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

    public const string ThirdQuestReference = "CH-I-QUEST-003";
    public const string ThirdQuestTitle = "The Missing Confirmation";
    public const string ThirdQuestDescription = "The confirmation status for the supporting documentation has not been copied onto the case summary.";
    public const string ThirdQuestInstructions = "Using the personal intake records and the Receipt Registration Procedure, identify the confirmation status that applies to the supporting documentation.";
    public const string ThirdTaskReference = "CH-I-TASK-003";
    public const string ThirdTaskTitle = "Identify the Confirmation Status";
    public const string ThirdTaskInstructions = "Using the personal intake records and the Receipt Registration Procedure, identify the confirmation status that applies to the supporting documentation.\n\nSubmit the confirmation status.";
    public const string ThirdAnswer = "ELECTRONIC RECEIPT ONLY";
    public const string ThirdSuccessMessage = "The confirmation status has been recorded. The preliminary finding is now available for examination.";
    public const string ThirdLockedDocument = "CH-I-DOC-015";

    public const string ArchiveCaseReference = "ARCHIVE-2019-047";
    public const string ArchiveQuestReference = "CH-I-QUEST-004";
    public const string ArchiveTaskReference = "CH-I-TASK-004";
    public const string ArchiveAnswer = "CL-19";

    public const string FeeCaseReference = "FILE-2026-088";
    public const string FeeQuestReference = "CH-I-QUEST-005";
    public const string FeeTaskReference = "CH-I-TASK-005";
    public const string FeeAnswer = "E-88-R";

    public static readonly ChapterOneQuest[] Quests =
    [
        new(QuestReference, QuestTitle, QuestDescription, QuestInstructions, true, null, QuestKinds.Main, new ChapterOneTask(TaskReference, TaskTitle, TaskInstructions, AcceptedAnswer, SuccessMessage, [LockedDocument, "CH-I-DOC-008", "CH-I-DOC-009", "CH-I-DOC-010"])),
        new(SecondQuestReference, SecondQuestTitle, SecondQuestDescription, SecondQuestInstructions, true, TaskReference, QuestKinds.Main, new ChapterOneTask(SecondTaskReference, SecondTaskTitle, SecondTaskInstructions, SecondAnswer, null, [SecondLockedDocument, "CH-I-DOC-012", "CH-I-DOC-013", "CH-I-DOC-014"])),
        new(ThirdQuestReference, ThirdQuestTitle, ThirdQuestDescription, ThirdQuestInstructions, true, SecondTaskReference, QuestKinds.Main, new ChapterOneTask(ThirdTaskReference, ThirdTaskTitle, ThirdTaskInstructions, ThirdAnswer, ThirdSuccessMessage, [ThirdLockedDocument])),
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
        new("CH-I-DOC-012", "Electronic Intake Extract", "Extract", "JUUL", true, "WEDDINGBANK\nPrivate Banking · Compliance\n\nELECTRONIC INTAKE EXTRACT\n\nReference: CI-2026-1030-07\nSubject: Supporting documentation\n\nIntake handling: Interim logging\n\nThis extract does not record the inspection verification.\nIt does not state a confirmation status.\n\nDistribution: Account Holder — Juul\n\nInternal administrative record."),
        new("CH-I-DOC-013", "Physical File Inspection", "Note", "AMBER", true, "WEDDINGBANK\nPrivate Banking · Compliance\n\nPHYSICAL FILE INSPECTION\n\nReference: CI-2026-1030-07\nSubject: Supporting documentation\n\nInspection verification: Exception without exhibit\n\nThis note does not record the intake handling.\nIt does not state a confirmation status.\n\nDistribution: Account Holder — Amber\n\nInternal administrative record."),
        new("CH-I-DOC-014", "Receipt Registration Procedure", "Register", "BOTH", true, "WEDDINGBANK\nPrivate Banking · Compliance\n\nRECEIPT REGISTRATION PROCEDURE\n\nThe confirmation status is taken only from the intake handling and the inspection verification. An intake exception does not itself determine the status. This procedure does not state which handling or verification applies to an open review.\n\nConfirmation status | Intake handling | Inspection verification\nELECTRONIC RECEIPT ONLY | Interim logging | Exception without exhibit\nELECTRONIC RECEIPT PENDING | Interim logging | Exhibit confirmed\nADMINISTRATIVE RECEIPT OPEN | Final logging | Exhibit confirmed\nADMINISTRATIVE RECEIPT OPEN | Final logging | Exception without exhibit\n\nNo other confirmation status is used.\n\nInternal administrative record."),
        new("CH-I-DOC-015", "Preliminary Review Finding", "Record", "BOTH", false, "WEDDINGBANK\nPrivate Banking · Compliance\n\nPRELIMINARY REVIEW FINDING\n\nReference: PR-2026-1106-01\nDate: 06 November 2026\nSubject: Supporting documentation — restricted allocation\n\nThe electronic registration of the supporting documentation exists.\n\nThe physical receipt is not proved. The physical whereabouts of the original document remain unknown.\n\nThe available information does not establish fraud or an intentional removal.\n\nThe internal review remains open. The restricted allocation of EUR 250.00 remains unavailable.\n\nThis finding records the present state of the review. It does not close the matter.\n\nInternal administrative record."),
    ];

    public static readonly ChapterOneDocument[] ArchiveDocuments =
    [
        new("ARCH-2019-001", "Client Records Extract", "Extract", "JUUL", true, "WEDDINGBANK\nPrivate Banking · Client Records\n\nCLIENT RECORDS EXTRACT\n\nFile: ARCHIVE-2019-047\n\nThe closure of this file was entered by Client Records.\n\nThe identity line reads: removed. The identity schedule was not attached to this extract.\n\nThis extract does not give the closure date or the closure code.\n\nDistribution: Account Holder — Juul\n\nInternal administrative record."),
        new("ARCH-2019-002", "Compliance Review Note", "Note", "AMBER", true, "WEDDINGBANK\nPrivate Banking · Compliance\n\nCOMPLIANCE REVIEW NOTE\n\nFile: ARCHIVE-2019-047\nDate of review: 12 March 2019\n\nIdentity verification: incomplete.\nSource-of-funds review: not performed.\n\nCompliance did not recommend closure.\n\nThe closure entry, where made, is dated 14 March 2019. This note does not name the department or the closure code.\n\nDistribution: Account Holder — Amber\n\nInternal administrative record."),
        new("ARCH-2019-003", "March 2019 Closure Register", "Register", "BOTH", true, "WEDDINGBANK\nPrivate Banking · Records Administration\n\nMARCH 2019 CLOSURE REGISTER\n\nDate | Department | Code\n14/03/2019 | Client Records | CL-19\n28/03/2019 | Client Records | CL-21\n14/03/2019 | Legal Affairs | CL-20\n02/04/2019 | Account Services | CL-18\n\nThe register records the closure entries. It does not explain the codes.\n\nSubsequent allocation reviews are indexed separately.\n\nInternal administrative record."),
        new("ARCH-2019-004", "Archive Retention Note", "Record", "BOTH", false, "WEDDINGBANK\nPrivate Banking · Records Administration\n\nARCHIVE RETENTION NOTE\n\nFile: ARCHIVE-2019-047\nClosure: 14 March 2019\nCode: CL-19\n\nCL-19 records an administrative closure. It does not require a completed identity verification or a completed source-of-funds review.\n\nCompliance had recorded the review as incomplete two days earlier. The file was closed nevertheless.\n\nThe former client's name remains removed. Schedule IDS-2019-047 is not in the file. The file does not state why the identity line was removed.\n\nNo entry in this file identifies a connection to a later allocation review.\n\nInternal administrative record."),
    ];

    public static readonly ChapterOneDocument[] FeeDocuments =
    [
        new("FILE-2026-001", "Hold Appearance Extract", "Extract", "JUUL", true, "WEDDINGBANK\nPrivate Banking · Account Services\n\nHOLD APPEARANCE EXTRACT\n\nAn entry was presented to the account as a temporary hold.\n\nOriginal booking reference: E-88\n\nThis extract records that reference only.\n\nDistribution: Account Holder — Juul\n\nInternal administrative record."),
        new("FILE-2026-002", "Correction Practice Note", "Note", "AMBER", true, "WEDDINGBANK\nPrivate Banking · Account Services\n\nCORRECTION PRACTICE NOTE\n\nAn apparent hold created by a duplicated custody fee is explained by the correcting entry, not by the open posting.\n\nThe correcting entry is cited as the original booking reference followed by -R.\n\nThis note does not identify the original booking reference.\n\nDistribution: Account Holder — Amber\n\nInternal administrative record."),
        new("FILE-2026-003", "Fee Ledger — November 2026", "Register", "BOTH", true, "WEDDINGBANK\nPrivate Banking · Account Services\n\nFEE LEDGER — NOVEMBER 2026\n\nEntry | Description | Amount | Position\nE-14 | Advisory fee | EUR 80.00 | Open\nE-88 | Custody fee | EUR 250.00 | Open\nE-91 | Custody fee | EUR 250.00 | Open\nE-14-R | Advisory fee | EUR 80.00 | Reversal\nE-88-R | Custody fee | EUR 250.00 | Reversal\nE-91-R | Custody fee | EUR 250.00 | Reversal\n\nInternal administrative record."),
        new("FILE-2026-004", "Posting Correction Note", "Record", "BOTH", false, "WEDDINGBANK\nPrivate Banking · Account Services\n\nPOSTING CORRECTION NOTE\n\nReference: E-88-R\nDate: 04 November 2026\n\nEntry E-88 was a duplicate custody fee of EUR 250.00. The fee had already been charged. The duplicate was reversed the same day by entry E-88-R.\n\nThe original line was left unchanged. The reversal is the correcting entry. No restriction was placed on the account. The amount was not an allocation, and it was not withheld.\n\nThe matter is closed.\n\nInternal administrative record."),
    ];

    public static readonly ChapterOneQuest[] SideQuests =
    [
        new(ArchiveQuestReference, "The Closed File", "A former relationship file was closed in March 2019. The departmental records do not agree on the completeness of the review.", "Using the personal extracts and the March 2019 Closure Register, identify the closure code entered for ARCHIVE-2019-047.", true, null, QuestKinds.Side, new ChapterOneTask(ArchiveTaskReference, "Identify the Closure Code", "Submit the closure code entered for ARCHIVE-2019-047.", ArchiveAnswer, "The closure entry has been identified. The retention note is now available for examination.", ["ARCH-2019-004"])),
        new(FeeQuestReference, "The Apparent Hold", "A fee entry was presented as a temporary hold. Several November postings could explain it.", "Using the personal notes and the November 2026 fee ledger, identify the entry that explains the apparent hold.", true, null, QuestKinds.Side, new ChapterOneTask(FeeTaskReference, "Identify the Explaining Entry", "Using the personal notes and the November 2026 fee ledger, submit the reference of the entry that explains the apparent hold.", FeeAnswer, "The posting has been identified. The correction note is now available for examination.", ["FILE-2026-004"])),
    ];

    public static readonly ChapterOneCase[] Cases =
    [
        new(CaseReference, CaseTitle, CaseSummary, CaseIntroduction, Documents, Quests),
        new(ArchiveCaseReference, "Former Relationship File", "A closed relationship file retains contradictory departmental records. The former client's identity is incomplete.", "The file was closed in 2019. It may be examined separately from the allocation now under review.", ArchiveDocuments, [SideQuests[0]]),
        new(FeeCaseReference, "Apparent Hold", "A posting of EUR 250.00 was briefly presented as a hold. The fee ledger records a separate explanation.", "This file concerns a custody-fee posting. It is not the restricted allocation.", FeeDocuments, [SideQuests[1]]),
    ];
}

public sealed record ChapterOneCase(
    string Reference,
    string Title,
    string Summary,
    string Introduction,
    ChapterOneDocument[] Documents,
    ChapterOneQuest[] Quests);

public sealed record ChapterOneQuest(
    string Reference,
    string Title,
    string Description,
    string Instructions,
    bool Published,
    string? ReleasedAfterTask,
    string Kind,
    ChapterOneTask Task);

public sealed record ChapterOneTask(
    string Reference,
    string Title,
    string Instructions,
    string AcceptedAnswer,
    string? SuccessMessage,
    string[] Unlocks);
