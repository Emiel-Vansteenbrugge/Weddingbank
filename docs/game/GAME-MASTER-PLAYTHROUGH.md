# Game master playthrough

Private notes for the person running Weddingbank. This file is the maintained solution and playthrough. It is not part of the client portal or the public website. The site image is built only from the frontend, and the API image is built only from the backend. Neither copies `docs/`.

Update this guide whenever a chapter, quest, task, document, hint, notification, unlock, or administrator action is added or changed. The guide has to stay consistent with the implementation. Do not invent a clue, answer, or hint here and leave the game unchanged.

Inspected on 5 November 2026 from the Chapter I source catalog and the local development database. Production progress was not inspected for this revision.

## Status labels

- **Published.** Participants can open it, subject to the audience and unlock rules below.
- **Draft, local only.** The rows exist in the local development database. They are not in the Chapter I initializer, so a new or deployed environment does not receive them until they are created there.
- **Not built.** The application has a place for it, or a future requirement mentions it, but there is no content and no working action.

## 1. Game overview and intended narrative

Weddingbank is a private-banking story for two participants, Juul and Amber. They share one restricted allocation. The house has made EUR 250.00 unavailable while it looks for a missing supporting record. Nothing on the public website is a puzzle clue. The public pages are the bank’s front door. Client access leads to sign-in.

The played story, as far as it exists, is Chapter I, “The Account”.

1. Both participants can read the shared account papers. The funds are restricted, the review is open, and the supporting document is missing from the electronic file even though a registry says it was received on 29 October 2026.
2. Juul alone is told the receipt was processed by Records Administration. Amber alone is told the registration date, 29 October 2026. Together, on the October 2026 Closing Register, that is registry entry `REG-1026-17`.
3. Solving that shared task releases the registry record. It says the original supporting record was transferred to departmental review on 30 October 2026, and that the receiving department has not confirmed physical receipt.
4. The second quest continues that sentence. Juul alone learns the dispatch batch, `TR-1030`. Amber alone learns the routing category, `B`. The Departmental Transfer Register then identifies `TR-1030-B`, received by Compliance.
5. Solving that shared task releases a Compliance intake exception: the transfer was recorded electronically, but the original paper is not in the physical file, and no final receipt has been issued.

No chapter after this is written. No side quest is written. No hint text is written.

## 2. Complete chapter progression

| Order | Reference | Title | State on 5 November 2026 | What it changes |
| --- | --- | --- | --- | --- |
| Chapter I | `CH-I-001` | The Account | Published. In the initializer. | The case both participants can open. |
| Quest 1 | `CH-I-QUEST-001` | The Missing Record | Published. In the initializer. | One shared task. |
| Task 1 | `CH-I-TASK-001` | Identify the Registry Entry | Published. In the initializer. | Accepts `REG-1026-17` and releases `CH-I-DOC-007`. |
| Quest 2 | `CH-I-QUEST-002` | The Unconfirmed Transfer | Draft, local only. | Hidden from participants until published. |
| Task 2 | `CH-I-TASK-002` | Identify the Receiving Department | Draft, local only. | Accepts `Compliance` and releases `CH-I-DOC-011`. |
| Chapter II | — | — | Not built. | No case, quest, document, or ending exists. |

Both quests are main quests. Both tasks are shared. There is no individual task.

## 3. Initial state of each participant account

| | Juul | Amber |
| --- | --- | --- |
| Sign-in name | `Juul-V` | `Amber-D` |
| Name shown in the house | Juul | Amber |
| Role | Client | Client |
| Individual tasks | None | None |
| Shared work at a fresh start | Quest 1 is open. Quest 2 is hidden while it remains a draft. | The same shared assignment. |
| Accounts page | Title “Private account”. Note “Nothing has been posted.” The EUR 250.00 figure is not on this page. It is only in `CH-I-DOC-001`. | Same, with Amber’s name. |
| Correspondence page | Fixed sentence: “The house has not written.” This page is not connected to hints. | Same. |
| Archive | Fixed sentence: “The archive has not been opened.” | Same. |

Passwords are not recorded in this guide. The administrator desk is where the house passwords are kept.

The local development database, inspected on 5 November 2026, still has both shared assignments incomplete and no submissions. The creator has reported that, on the live play, puzzle 1 was solved and `CH-I-DOC-007` was unlocked. That live progress was not re-read for this guide. A fresh play, or a future game reset, starts from the table above.

The dashboard stage uses individual tasks and activity rows only. Both current tasks are shared, and submitting an answer does not write an activity. With no individual task and no activity, the progress line stays “Not started” even after a shared task is solved. The other lines still change. See section 9.

## 4. Everything a participant can encounter

### Opening case

Participants see this once the case is published and their audience includes them. Both are included.

- Reference: `CH-I-001`
- Title: The Account
- Summary: “An administrative review has placed an unusual allocation of funds under restriction. Initial records appear incomplete, and further verification is required.”
- Introduction: “The account holders have received notification that their allocated funds remain unavailable pending an internal review.”

### Documents

A document is visible when all of these are true:

- The case is published for that participant.
- The document audience includes that participant. `BOTH` includes both. `JUUL` is Juul only. `AMBER` is Amber only.
- The document is published, or it has an unlock rule.
- It is not still locked. A document with an unlock rule stays hidden until a completed assignment for that participant covers the linked task.

`CH-I-DOC-007` and `CH-I-DOC-011` stay drafts on purpose. The unlock releases them. Publishing them is not required. While the linked task is unfinished, they stay hidden even if someone publishes them, because the lock still applies.

Descriptions stored on these documents are empty. The text below is the document body.

#### CH-I-DOC-001 — Account Statement — 04 November 2026

Published. Audience both. Category Statement. In the initializer.

```text
Account holder: The account holders
Account status: Restricted
Allocated funds: EUR 250.00
Available funds: EUR 0.00
Restricted funds: EUR 250.00

Funds temporarily unavailable.

This statement is provided for informational purposes. The restricted allocation remains subject to internal review.
```

#### CH-I-DOC-002 — Official Account Notice

Published. Audience both. Category Notice. In the initializer. This is the opening account notification.

```text
WEDDINGBANK
Private Banking · Account Services

OFFICIAL ACCOUNT NOTICE

Reference: AS-2026-1104-02
Date: 04 November 2026
Subject: Temporary Restriction of Allocated Funds

Dear Account Holder,

We hereby confirm that the amount of EUR 250.00 allocated to your private account is currently subject to an administrative restriction.

The restriction was initiated following an internal review concerning the allocation and its supporting records.

At this time, the review remains open. The relevant departments have been instructed to verify the available documentation before any further action can be taken.

Until this review has been concluded, the allocated funds will remain unavailable.

Please retain this notice for your records. No further action is required from you at this stage.

Yours faithfully,

Account Services
Weddingbank

This document is intended solely for the named account holder.
```

#### CH-I-DOC-003 — Internal Processing Note

Published. Audience both. Category Note. In the initializer.

```text
WEDDINGBANK
Private Banking · Records Administration

INTERNAL PROCESSING NOTE

Reference: RA-2026-1103-08
Date: 03 November 2026
Subject: Supporting documentation — allocation review

The supporting documentation relating to the restricted allocation has been reviewed against the current registry.

The registry indicates that the relevant documentation was received and registered on 29 October 2026.

However, no corresponding document could be located in the electronic case file at the time of this review.

The receipt status therefore remains recorded as acknowledged — confirmation pending.

Further verification has been requested before the internal review can be concluded.

Distribution: Account Services
Prepared by: Records Administration

Internal administrative record. Not intended for external distribution.
```

#### CH-I-DOC-004 — Registry Extract — Internal Reference

Published. Audience Juul only. Category Extract. In the initializer. Amber must not see it.

```text
WEDDINGBANK
Private Banking · Account Services

REGISTRY EXTRACT — INTERNAL REFERENCE

The original receipt was processed by Records Administration.

The corresponding registry entry was filed separately from the supporting documentation.

Please refer to the October 2026 closing register.

Distribution: Account Holder — Juul
```

#### CH-I-DOC-005 — Document Receipt Confirmation

Published. Audience Amber only. Category Confirmation. In the initializer. Juul must not see it.

```text
WEDDINGBANK
Private Banking · Records Administration

DOCUMENT RECEIPT CONFIRMATION

The relevant supporting documentation was registered on 29 October 2026.

The original reference number was not transferred to the electronic case file.

Distribution: Account Holder — Amber
```

#### CH-I-DOC-006 — October 2026 Closing Register

Published. Audience both. Category Register. In the initializer.

```text
Reference | Department | Date | Subject
REG-1026-14 | Account Services | 28/10/2026 | Account review
REG-1026-17 | Records Administration | 29/10/2026 | Supporting documentation
REG-1026-19 | Compliance | 29/10/2026 | Identity verification
REG-1026-21 | Records Administration | 30/10/2026 | Correspondence
```

#### CH-I-DOC-007 — Supporting Documentation — Registry Record

Draft. Audience both. Category Record. In the initializer. Hidden until `CH-I-TASK-001` is complete. Then both participants can read it.

```text
WEDDINGBANK
Private Banking · Records Administration

REGISTRY RECORD

Reference: REG-1026-17
Subject: Supporting Documentation

The original supporting record was transferred to departmental review on 30 October 2026.

The receiving department has not yet confirmed physical receipt.

Further verification remains pending.

Internal administrative record.
```

#### CH-I-DOC-008 — Dispatch Control Extract

Draft, local only. Audience Juul only. Category Extract. Amber must not see it. It does not name Compliance.

```text
WEDDINGBANK
Private Banking · Records Administration

DISPATCH CONTROL EXTRACT

The transfer relating to the supporting documentation was included in dispatch batch TR-1030.

The batch contains several departmental movements.

The receiving department is not identified in this extract.

Distribution: Account Holder — Juul
```

#### CH-I-DOC-009 — Routing Classification Notice

Draft, local only. Audience Amber only. Category Notice. Juul must not see it. It does not name Compliance.

```text
WEDDINGBANK
Private Banking · Records Administration

ROUTING CLASSIFICATION NOTICE

The supporting record was assigned routing category B before dispatch.

This classification identifies the processing route used for the transfer.

It does not independently identify the receiving department.

Distribution: Account Holder — Amber
```

#### CH-I-DOC-010 — Departmental Transfer Register

Draft, local only. Audience both. Category Register. This is the shared register for puzzle 2.

```text
WEDDINGBANK
Private Banking · Records Administration

DEPARTMENTAL TRANSFER REGISTER

Reference | Routing Category | Receiving Department | Subject

TR-1030-A | A | Account Services | Account review
TR-1030-B | B | Compliance | Supporting documentation
TR-1030-C | C | Legal Affairs | Supporting documentation
TR-1031-B | B | Records Administration | Correspondence

Internal administrative record.
```

#### CH-I-DOC-011 — Compliance Intake Exception

Draft, local only. Audience both. Category Record. Hidden until `CH-I-TASK-002` is complete. Then both participants can read it.

```text
WEDDINGBANK
Private Banking · Compliance

COMPLIANCE INTAKE EXCEPTION

Reference: CI-2026-1030-07
Subject: Supporting Documentation

The transfer associated with reference TR-1030-B appears in the departmental intake register.

The item was recorded as electronically received.

However, the original supporting record could not be located in the corresponding physical case file.

The intake entry therefore remains subject to verification.

No final receipt confirmation has been issued.

Internal administrative record.
```

### Notifications, correspondence, and other channels

| Item | State |
| --- | --- |
| Opening account notification | `CH-I-DOC-002`, already published. There is no separate message row. |
| Hints | Not built as a sending action. The administrator page is a fixed empty sentence. The local database has no hint rows. No hint wording exists. |
| Messages from the house | The data model can store one, but nothing sends or displays it. The correspondence page always says “The house has not written.” |
| Telephone requests | Not built. Participants cannot file one. The administrator page is a fixed empty sentence. The local database has no phone-request rows. |
| Public contact form | A public enquiry form. It is not a game message and is not a clue. |
| Activities | None stored locally. The game does not record a solved task as an activity. |

## 5. Quests and tasks

Answers ignore capital letters and surrounding spaces. The remaining text must match exactly. `reg-1026-17` and ` Compliance ` are accepted. `REG-1026-17.` and `Compliance Department` are refused. A refused answer does not complete the task and does not release a document.

The refusal shown for every task, including the department question, is: “The submitted reference could not be verified.”

Either participant may submit a shared answer. One shared assignment exists per task. When it is completed, both participants see it completed.

### CH-I-QUEST-001 — The Missing Record

- Kind: main.
- Audience: both.
- State: published. In the initializer.
- Case: `CH-I-001`.
- Participant-facing description: “An inconsistency has been identified in the records relating to the restricted allocation. Further examination of the October closing register may clarify where the missing supporting documentation was recorded.”
- Participant-facing instructions: “Use the information available in your personal account documents and the October 2026 Closing Register to identify the correct registry entry.”

#### CH-I-TASK-001 — Identify the Registry Entry

- Scope: shared. No single participant owns it.
- State: published. In the initializer.
- Task type: text.
- Participant-facing instructions: “Submit the reference number of the registry entry that matches the department, date and subject identified in the available records.”
- Accepted answer: `REG-1026-17`
- Success message: “The registry entry has been identified. The associated record is now available for examination.”
- Unlock: `CH-I-DOC-007`

Information each person holds:

- Juul, from `CH-I-DOC-004`: the receipt was processed by Records Administration. He is pointed at the October 2026 closing register. He is not given the date.
- Amber, from `CH-I-DOC-005`: the supporting documentation was registered on 29 October 2026. She is not given the department or the reference number.
- Both, from `CH-I-DOC-003`: the registry date is 29 October 2026, the electronic file does not contain the document, and the subject is the supporting documentation. This note does not say that Records Administration processed the receipt. It does say the note was prepared by Records Administration.
- Both, from `CH-I-DOC-006`: the four registry rows.

Step-by-step solution:

1. Juul reads that the department is Records Administration.
2. Amber reads that the registration date is 29 October 2026.
3. On `CH-I-DOC-006`, the only row with department Records Administration and date 29/10/2026 is `REG-1026-17`, subject “Supporting documentation”.
4. Submit `REG-1026-17`.
5. The shared task completes and `CH-I-DOC-007` opens for both.

Game-master note on the current wording: `CH-I-DOC-003` already gives both participants the date 29 October 2026. Juul can reach `REG-1026-17` from his own department clue plus that shared date, without Amber’s paper. The intended play is still the exchange above. Amber cannot finish from her paper alone, because two rows are dated 29/10/2026 and her paper does not name the department.

Hints: none written. Reveal order: not defined.

### CH-I-QUEST-002 — The Unconfirmed Transfer

- Kind: main.
- Audience: both.
- State: draft, local only. Participants do not see it.
- Case: `CH-I-001`.
- Participant-facing description: “The supporting record was transferred to departmental review on 30 October 2026. The receiving department has not confirmed physical receipt.”
- Participant-facing instructions: “Using the available personal records and the Departmental Transfer Register, identify the department that received the supporting documentation associated with the transfer.”

#### CH-I-TASK-002 — Identify the Receiving Department

- Scope: shared. No single participant owns it.
- State: draft, local only.
- Task type: text.
- Participant-facing instructions: “Using the available personal records and the Departmental Transfer Register, identify the department that received the supporting documentation associated with the transfer. Submit the name of the receiving department.”
- Accepted answer: `Compliance`
- Success message: none stored. The application then shows “The answer has been accepted.”
- Unlock: `CH-I-DOC-011`
- The answer to submit is the department name, not the register reference. `TR-1030-B` is refused.

Information each person holds, once the draft documents are published:

- Juul, from `CH-I-DOC-008`: dispatch batch `TR-1030`. The extract says the receiving department is not identified. Inside that batch the register has three departments.
- Amber, from `CH-I-DOC-009`: routing category `B`. The notice says this does not by itself identify the receiving department. Category B has two rows.
- Both, from `CH-I-DOC-010`: the four transfer rows.
- Both, after puzzle 1, from `CH-I-DOC-007`: the transfer happened on 30 October 2026 and physical receipt is unconfirmed. That document names neither the batch, the category, nor Compliance.

Step-by-step solution:

1. Juul contributes batch `TR-1030`.
2. Amber contributes routing category `B`.
3. The only row that is both batch `TR-1030` and category `B` is `TR-1030-B`.
4. The receiving department on that row is Compliance. The subject is “Supporting documentation”.
5. Submit `Compliance`.
6. The shared task completes and `CH-I-DOC-011` opens for both.

Hints: none written. Reveal order: not defined.

## 6. Unlock relationships and resulting state

| Completed task | Assignment | Document released | Who can then read it | Document status after release |
| --- | --- | --- | --- | --- |
| `CH-I-TASK-001` | One shared assignment | `CH-I-DOC-007` | Juul and Amber | Remains draft. The rule releases it. |
| `CH-I-TASK-002` | One shared assignment | `CH-I-DOC-011` | Juul and Amber | Remains draft. The rule releases it. |

No other unlock exists. A wrong answer changes nothing except that a refused submission is stored. Completing the shared assignment updates both participants at once. There is no separate Amber assignment to complete.

`CH-I-DOC-007` is the bridge into puzzle 2. It does not reveal `TR-1030`, category `B`, or Compliance.

`CH-I-DOC-011` confirms `TR-1030-B`, says the item was received electronically, and says the original paper is missing and no final receipt was issued. It does not add another task.

## 7. Optional content and red herrings

No optional quest, document, or hint exists.

Rows that are present so the answer is not the only line on the page:

| Source | Row | Why it is not the answer |
| --- | --- | --- |
| `CH-I-DOC-006` | `REG-1026-14`, Account Services, 28/10/2026, Account review | Wrong department, date, and subject. |
| `CH-I-DOC-006` | `REG-1026-19`, Compliance, 29/10/2026, Identity verification | Same date as the real entry, wrong department and subject. This is the only published use of the word Compliance before puzzle 2. A participant can see that word before puzzle 2 is published. It is not the registry answer, and it does not identify the later receiving department. |
| `CH-I-DOC-006` | `REG-1026-21`, Records Administration, 30/10/2026, Correspondence | Right department, wrong date and subject. |
| `CH-I-DOC-010` | `TR-1030-A`, category A, Account Services, Account review | Right batch, wrong category. |
| `CH-I-DOC-010` | `TR-1030-C`, category C, Legal Affairs, Supporting documentation | Right batch and the same subject as the real row, wrong category. |
| `CH-I-DOC-010` | `TR-1031-B`, category B, Records Administration, Correspondence | Right category, wrong batch and subject. |

`CH-I-DOC-003` giving both participants the date is recorded in section 5. It is part of the published text, not a separate optional document.

## 8. Administrator actions and when they happen

Actions the house can take now:

| When | Action |
| --- | --- |
| Before the first play | Confirm the case, documents `CH-I-DOC-001` through `CH-I-DOC-006`, quest 1, and task 1 are published. Leave `CH-I-DOC-007` as a draft. The initializer can create or repair that published set. It does not create puzzle 2. |
| Before puzzle 2 may be played | Puzzle 2 is still a draft and, in this revision, exists only in the local development database. To make that local copy playable, publish `CH-I-QUEST-002`, `CH-I-TASK-002`, `CH-I-DOC-008`, `CH-I-DOC-009`, and `CH-I-DOC-010`. Leave `CH-I-DOC-011` as a draft. Do this only after the draft has been accepted. Do not treat a deploy as publishing puzzle 2. |
| During play | Read submissions in the quest. A refused submission stays on the task. Do not complete an assignment by hand. |
| If they are stuck | No hint can be sent from the administrator screens. Any help would be outside the application, and no hint text has been approved. |
| Opening a participant desk | From the administrator desk, opening Juul or Amber signs the administrator in as that participant. Signing out and signing in again is required to return. This does not reset progress. |
| Temporary test switch | The public site can currently switch among Juul, Amber, and the administrator without a password. That switch is for testing. Turn it off before the wedding. It is not a story event. |
| Passwords | Change them from the house-account screen if needed. Do not record the new passwords in this guide. |

Actions that are not available:

| Action | State |
| --- | --- |
| Send a hint or a house message | Not built. |
| Receive or handle a phone request | Not built. |
| Reset both participants to a fresh game while keeping the content | Required later, not built. A database restore is not that reset. |
| Start Chapter II | Not built. No content is planned in the repository. |

## 9. Expected participant progress after each step

Counts below are what the participant dashboard should show. “Shared” is the shared line. “Tasks open” is the tasks line, which adds individual open tasks and shared open tasks. Individual open and individual completed stay at 0 throughout Chapter I, because no individual task exists.

The administrator overview counts every assignment, including drafts. The participant dashboard does not. While puzzle 2 remains a draft and unsolved, the administrator shared line still includes that open draft and the participant line does not.

| After this step | Juul and Amber, shared | Juul and Amber, tasks open | Documents newly visible | Administrator shared line, current draft included |
| --- | --- | --- | --- | --- |
| Fresh start, puzzle 2 still draft, puzzle 1 unsolved | 0 completed, 1 open | 1 open | 001, 002, 003, 006 for both; 004 for Juul; 005 for Amber | 0 completed, 2 open |
| Puzzle 1 solved, puzzle 2 still draft | 1 completed, 0 open | Nothing is waiting | `CH-I-DOC-007` for both | 1 completed, 1 open |
| Puzzle 2 published, not yet solved, puzzle 1 already solved | 1 completed, 1 open | 1 open | 008 for Juul, 009 for Amber, 010 for both. 011 still hidden. | 1 completed, 1 open |
| Both puzzles solved | 2 completed, 0 open | Nothing is waiting | `CH-I-DOC-011` for both | 2 completed, 0 open |

Headline on the dashboard at a fresh start: “Shared work is waiting.” The progress section can still say “Not started”, for the reason in section 3. After the only open shared task is solved, and no individual task exists, the headline becomes “Your matter is not started.” The shared line is the reliable progress line.

If puzzle 1 has not been solved when puzzle 2 is published, the shared line is 0 completed and 2 open, and the tasks line is 2 open. That is the local development position if both quests were published today. The live play was reported as already past puzzle 1. This guide did not confirm that live database.

## 10. End-to-end playthrough checklist

Use a fresh pair of accounts, or a future reset, before treating this as the wedding-day run. Do not use this checklist to publish puzzle 2 until that draft has been accepted.

- [ ] Public site opens. No puzzle reference appears there.
- [ ] Juul signs in as Juul and sees “Welcome, Juul.” Amber signs in as Amber.
- [ ] Juul cannot open Amber’s session, and Amber cannot open the administrator desk.
- [ ] Accounts says “Private account” and “Nothing has been posted.”
- [ ] Correspondence says “The house has not written.” Archive says “The archive has not been opened.”
- [ ] Both see case `CH-I-001`, The Account.
- [ ] Both see documents 001, 002, 003, and 006.
- [ ] Only Juul sees 004. Only Amber sees 005.
- [ ] Neither sees 007, 008, 009, 010, or 011.
- [ ] Tasks shows only The Missing Record, with one open shared task.
- [ ] Dashboard shared line: 0 completed, 1 open. Tasks line: 1 open.
- [ ] A wrong registry number is refused with “The submitted reference could not be verified.” Document 007 stays hidden. The task stays open for both.
- [ ] `REG-1026-17` is accepted. Both see the task complete, the success message, and document 007.
- [ ] Document 007 does not name a receiving department, a batch, or a routing category.
- [ ] The Unconfirmed Transfer is still absent while it is a draft.
- [ ] Publish quest 002, task 002, and documents 008, 009, and 010. Leave 011 draft.
- [ ] Juul sees 008 and 010, and does not see 009 or 011.
- [ ] Amber sees 009 and 010, and does not see 008 or 011.
- [ ] Neither personal paper names Compliance.
- [ ] A wrong department, including `TR-1030-B`, is refused. Document 011 stays hidden for both.
- [ ] `Compliance` is accepted. Both see the shared task complete and both can read document 011.
- [ ] Document 011 cites `TR-1030-B` and does not create another task.
- [ ] Dashboard shared line: 2 completed, 0 open. Tasks line: nothing waiting.
- [ ] Administrator overview matches those two completed shared tasks and does not show a leftover draft assignment as open.
- [ ] No hint, phone request, or Chapter II item appeared, because none exists.
- [ ] The temporary test-account switch is off before participants use the wedding-day site.

## Maintenance log

| Date | What changed in the guide |
| --- | --- |
| 5 November 2026 | First playthrough. Chapter I documents 001–011, quests 001 and 002, and both tasks, taken from the initializer and the local development database. Puzzle 2 marked draft and local only. Hints, phone requests, correspondence delivery, archive content, game reset, and Chapter II marked not built. |
