import { useEffect, useState, type FormEvent } from 'react'
import { SiteLink } from '../../shared/SiteLink.tsx'
import { visit } from '../auth/api.ts'
import { audienceLabel, fetchCase, fetchCases, type CaseSummary, type DocumentSummary } from '../cases/case-api.ts'
import {
  createQuest,
  createTask,
  fetchQuest,
  fetchQuests,
  fetchSubmissions,
  kindLabel,
  outcomeLabel,
  scopeLabel,
  setQuestStatus,
  setTaskStatus,
  statusLabel,
  updateQuest,
  updateTask,
  type QuestDetail,
  type QuestSummary,
  type SubmissionRecord,
  type TaskDetail,
} from '../quests/quest-api.ts'

export function QuestList() {
  const [quests, setQuests] = useState<QuestSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    fetchQuests()
      .then((rows) => {
        if (active) setQuests(rows)
      })
      .catch((caught: unknown) => {
        if (active) setError(caught instanceof Error ? caught.message : 'The quests could not be read.')
      })
    return () => {
      active = false
    }
  }, [])

  if (error && !quests) return <p className="section-note" role="alert">{error}</p>
  if (!quests) return <p className="section-note">Reading the quests…</p>

  return (
    <>
      <div className="ledger-head">
        <div>
          <p className="eyebrow">Quests</p>
          <h1>Quests</h1>
        </div>
        <SiteLink href="/admin/tasks/new" className="text-link">
          New quest
        </SiteLink>
      </div>
      <p className="section-note">Drafts and archived quests stay off the participants' desks. Accepted answers stay on this side of the house.</p>
      {quests.length === 0 ? <p className="section-note">No quests have been written.</p> : <QuestCatalogue quests={quests} />}
    </>
  )
}

export function QuestCatalogue({ quests }: { quests: QuestSummary[] }) {
  return (
    <div className="table-wrap">
      <table className="audit-table">
        <thead>
          <tr>
            <th>Reference</th>
            <th>Title</th>
            <th>Audience</th>
            <th>Type</th>
            <th>Status</th>
            <th>Progress</th>
          </tr>
        </thead>
        <tbody>
          {quests.map((item) => (
            <tr key={item.id}>
              <td>
                <SiteLink href={`/admin/tasks/${item.id}`} className="text-link">
                  {item.reference}
                </SiteLink>
              </td>
              <td>{item.title}</td>
              <td>{audienceLabel(item.audience)}</td>
              <td>{kindLabel(item.kind)}</td>
              <td>{statusLabel(item.status)}</td>
              <td>{item.completedAssignments} completed, {item.openAssignments} open</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export function QuestEditor({ questId }: { questId: string }) {
  const creating = questId === 'new'
  const [cases, setCases] = useState<CaseSummary[]>([])
  const [record, setRecord] = useState<QuestDetail | null>(null)
  const [submissions, setSubmissions] = useState<SubmissionRecord[]>([])
  const [reference, setReference] = useState('')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [instructions, setInstructions] = useState('')
  const [caseId, setCaseId] = useState('')
  const [audience, setAudience] = useState('BOTH')
  const [kind, setKind] = useState('MAIN')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)
  const [ready, setReady] = useState(creating)

  useEffect(() => {
    let active = true
    fetchCases()
      .then((rows) => {
        if (active) setCases(rows)
      })
      .catch(() => {
        if (active) setError('The cases could not be read.')
      })
    if (creating) return () => { active = false }
    fetchQuest(questId)
      .then(async (item) => {
        if (!active) return
        apply(item)
        setSubmissions(await fetchSubmissions(item.id))
        setReady(true)
      })
      .catch((caught: unknown) => {
        if (active) setError(caught instanceof Error ? caught.message : 'That quest could not be read.')
      })
    return () => {
      active = false
    }
  }, [creating, questId])

  function apply(item: QuestDetail) {
    setRecord(item)
    setReference(item.reference)
    setTitle(item.title)
    setDescription(item.description)
    setInstructions(item.instructions)
    setCaseId(item.caseId)
    setAudience(item.audience)
    setKind(item.kind)
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    if (pending) return
    setPending(true)
    setError(null)
    const draft = { reference, title, description, instructions, caseId, audience, kind }
    try {
      const saved = creating ? await createQuest(draft) : await updateQuest(questId, draft)
      apply(saved)
      if (creating) visit(`/admin/tasks/${saved.id}`)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The quest could not be saved.')
    } finally {
      setPending(false)
    }
  }

  async function changeStatus(status: string) {
    if (pending || !record) return
    setPending(true)
    setError(null)
    try {
      apply(await setQuestStatus(record.id, status))
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The quest status could not be changed.')
    } finally {
      setPending(false)
    }
  }

  if (error && !ready) return <p className="section-note" role="alert">{error}</p>
  if (!ready) return <p className="section-note">Reading the quest…</p>

  return (
    <>
      <p className="eyebrow">Quest</p>
      <h1>{creating ? 'New quest' : title || 'Quest'}</h1>
      <p className="section-note">
        <SiteLink href="/admin/tasks" className="text-link">All quests</SiteLink>
      </p>
      {record ? <p className="section-note">Status: {statusLabel(record.status)}. {record.caseReference}</p> : null}
      {error ? <p className="form-status" role="alert">{error}</p> : null}
      <form className="enquiry" onSubmit={save}>
        <label>
          Reference
          <input value={reference} onChange={(event) => setReference(event.target.value)} required />
        </label>
        <label>
          Title
          <input value={title} onChange={(event) => setTitle(event.target.value)} required />
        </label>
        <label>
          Description
          <textarea value={description} onChange={(event) => setDescription(event.target.value)} rows={4} required />
        </label>
        <label>
          Instructions
          <textarea value={instructions} onChange={(event) => setInstructions(event.target.value)} rows={5} required />
        </label>
        <label>
          Case
          <select value={caseId} onChange={(event) => setCaseId(event.target.value)} required>
            <option value="">Choose a case</option>
            {cases.map((item) => (
              <option key={item.id} value={item.id}>{item.reference} — {item.title}</option>
            ))}
          </select>
        </label>
        <label>
          Audience
          <select value={audience} onChange={(event) => setAudience(event.target.value)}>
            <option value="JUUL">Juul</option>
            <option value="AMBER">Amber</option>
            <option value="BOTH">Both</option>
          </select>
        </label>
        <label>
          Type
          <select value={kind} onChange={(event) => setKind(event.target.value)}>
            <option value="MAIN">Main</option>
            <option value="SIDE">Side</option>
          </select>
        </label>
        <button type="submit" disabled={pending}>{pending ? 'Saving' : 'Save'}</button>
      </form>
      {record ? (
        <div className="row-actions">
          {record.status !== 'PUBLISHED' ? <button className="quiet-button" type="button" onClick={() => void changeStatus('PUBLISHED')} disabled={pending}>Publish</button> : null}
          {record.status !== 'DRAFT' ? <button className="quiet-button" type="button" onClick={() => void changeStatus('DRAFT')} disabled={pending}>Return to draft</button> : null}
          {record.status !== 'ARCHIVED' ? <button className="quiet-button" type="button" onClick={() => void changeStatus('ARCHIVED')} disabled={pending}>Archive</button> : null}
        </div>
      ) : null}
      {record ? <TaskTable quest={record} /> : null}
      {record ? <SubmissionTable rows={submissions} /> : null}
    </>
  )
}

function TaskTable({ quest }: { quest: QuestDetail }) {
  return (
    <>
      <div className="ledger-head">
        <h2>Tasks</h2>
        <SiteLink href={`/admin/tasks/${quest.id}/tasks/new`} className="text-link">New task</SiteLink>
      </div>
      {quest.tasks.length === 0 ? <p className="section-note">No tasks have been written.</p> : (
        <div className="table-wrap">
          <table className="audit-table">
            <thead>
              <tr>
                <th>Reference</th>
                <th>Title</th>
                <th>Scope</th>
                <th>Status</th>
                <th>Progress</th>
                <th>Unlock</th>
              </tr>
            </thead>
            <tbody>
              {quest.tasks.map((task) => (
                <tr key={task.id}>
                  <td>
                    <SiteLink href={`/admin/tasks/${quest.id}/tasks/${task.id}`} className="text-link">{task.reference}</SiteLink>
                  </td>
                  <td>{task.title}</td>
                  <td>{scopeLabel(task.scope, task.participantLogin)}</td>
                  <td>{statusLabel(task.status)}</td>
                  <td>{task.completed ? 'Completed' : 'Open'}</td>
                  <td>{unlockText(task)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

function unlockText(task: TaskDetail) {
  if (task.unlocks.length === 0) return 'None'
  return task.unlocks.map((item) => `${item.reference || 'Document'} ${item.released ? 'released' : 'held'}`).join(', ')
}

export function SubmissionTable({ rows }: { rows: SubmissionRecord[] }) {
  return (
    <>
      <h2>Submissions</h2>
      {rows.length === 0 ? <p className="section-note">No answers have been submitted.</p> : (
        <div className="table-wrap">
          <table className="audit-table">
            <thead>
              <tr>
                <th>When</th>
                <th>Task</th>
                <th>Scope</th>
                <th>Submitted by</th>
                <th>Answer</th>
                <th>Outcome</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.id}>
                  <td>{new Date(row.submittedAt).toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' })}</td>
                  <td>{row.taskReference}</td>
                  <td>{scopeLabel(row.scope, row.assignedTo)}</td>
                  <td>{row.submittedBy}</td>
                  <td>{row.answer}</td>
                  <td>{outcomeLabel(row.outcome)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

export function TaskEditor({ questId, taskId }: { questId: string; taskId: string }) {
  const creating = taskId === 'new'
  const [quest, setQuest] = useState<QuestDetail | null>(null)
  const [documents, setDocuments] = useState<DocumentSummary[]>([])
  const [record, setRecord] = useState<TaskDetail | null>(null)
  const [reference, setReference] = useState('')
  const [title, setTitle] = useState('')
  const [instructions, setInstructions] = useState('')
  const [scope, setScope] = useState('SHARED')
  const [participant, setParticipant] = useState('')
  const [answers, setAnswers] = useState('')
  const [ignoreCase, setIgnoreCase] = useState(true)
  const [trimWhitespace, setTrimWhitespace] = useState(true)
  const [successMessage, setSuccessMessage] = useState('')
  const [unlocks, setUnlocks] = useState<string[]>([])
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)
  const [ready, setReady] = useState(false)

  useEffect(() => {
    let active = true
    fetchQuest(questId)
      .then(async (item) => {
        if (!active) return
        setQuest(item)
        const file = await fetchCase(item.caseId)
        if (!active) return
        setDocuments(file.documents)
        const task = item.tasks.find((entry) => entry.id === taskId)
        if (task) apply(task)
        setReady(true)
      })
      .catch((caught: unknown) => {
        if (active) setError(caught instanceof Error ? caught.message : 'That task could not be read.')
      })
    return () => {
      active = false
    }
  }, [questId, taskId])

  function apply(task: TaskDetail) {
    setRecord(task)
    setReference(task.reference)
    setTitle(task.title)
    setInstructions(task.instructions)
    setScope(task.scope)
    setParticipant(task.participantLogin === 'Amber-D' ? 'AMBER' : task.participantLogin ? 'JUUL' : '')
    setAnswers(task.answers.join('\n'))
    setIgnoreCase(task.ignoreCase)
    setTrimWhitespace(task.trimWhitespace)
    setSuccessMessage(task.successMessage ?? '')
    setUnlocks(task.unlocks.map((item) => item.documentId))
  }

  function draft() {
    return {
      reference,
      title,
      instructions,
      taskType: 'TEXT',
      scope,
      participant: scope === 'SHARED' ? '' : participant,
      answers: answers.split('\n'),
      ignoreCase,
      trimWhitespace,
      successMessage,
      unlockDocumentIds: unlocks,
    }
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    if (pending) return
    setPending(true)
    setError(null)
    try {
      const saved = creating ? await createTask(questId, draft()) : await updateTask(questId, taskId, draft())
      apply(saved)
      if (creating) visit(`/admin/tasks/${questId}/tasks/${saved.id}`)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The task could not be saved.')
    } finally {
      setPending(false)
    }
  }

  async function changeStatus(status: string) {
    if (pending || !record) return
    setPending(true)
    setError(null)
    try {
      apply(await setTaskStatus(questId, record.id, status))
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The task status could not be changed.')
    } finally {
      setPending(false)
    }
  }

  function toggleUnlock(id: string) {
    setUnlocks((current) => current.includes(id) ? current.filter((item) => item !== id) : [...current, id])
  }

  if (error && !ready) return <p className="section-note" role="alert">{error}</p>
  if (!ready || !quest) return <p className="section-note">Reading the task…</p>

  return (
    <>
      <p className="eyebrow">Task</p>
      <h1>{creating ? 'New task' : title || 'Task'}</h1>
      <p className="section-note">
        <SiteLink href={`/admin/tasks/${quest.id}`} className="text-link">Back to {quest.reference}</SiteLink>
      </p>
      {record ? <p className="section-note">Status: {statusLabel(record.status)}. {record.completed ? 'Completed' : 'Open'}.</p> : null}
      {error ? <p className="form-status" role="alert">{error}</p> : null}
      <form className="enquiry" onSubmit={save}>
        <label>
          Reference
          <input value={reference} onChange={(event) => setReference(event.target.value)} required />
        </label>
        <label>
          Title
          <input value={title} onChange={(event) => setTitle(event.target.value)} required />
        </label>
        <label>
          Instructions
          <textarea value={instructions} onChange={(event) => setInstructions(event.target.value)} rows={5} required />
        </label>
        <label>
          Assignment
          <select value={scope} onChange={(event) => setScope(event.target.value)}>
            <option value="SHARED">Shared</option>
            <option value="INDIVIDUAL">Individual</option>
          </select>
        </label>
        {scope === 'INDIVIDUAL' ? (
          <label>
            Participant
            <select value={participant} onChange={(event) => setParticipant(event.target.value)} required>
              <option value="">Choose</option>
              <option value="JUUL">Juul</option>
              <option value="AMBER">Amber</option>
            </select>
          </label>
        ) : null}
        <label>
          Accepted answers
          <textarea value={answers} onChange={(event) => setAnswers(event.target.value)} rows={4} required />
        </label>
        <label>
          Letter case
          <select value={ignoreCase ? 'yes' : 'no'} onChange={(event) => setIgnoreCase(event.target.value === 'yes')}>
            <option value="yes">Ignore letter case</option>
            <option value="no">Match letter case</option>
          </select>
        </label>
        <label>
          Surrounding spaces
          <select value={trimWhitespace ? 'yes' : 'no'} onChange={(event) => setTrimWhitespace(event.target.value === 'yes')}>
            <option value="yes">Ignore surrounding spaces</option>
            <option value="no">Keep surrounding spaces</option>
          </select>
        </label>
        <label>
          Confirmation
          <textarea value={successMessage} onChange={(event) => setSuccessMessage(event.target.value)} rows={3} />
        </label>
        <fieldset>
          <legend>Unlock on completion</legend>
          {documents.length === 0 ? <p className="section-note">This case has no documents.</p> : documents.map((item) => (
            <label key={item.id}>
              <input type="checkbox" checked={unlocks.includes(item.id)} onChange={() => toggleUnlock(item.id)} />
              {item.reference} — {item.title}
            </label>
          ))}
        </fieldset>
        <button type="submit" disabled={pending}>{pending ? 'Saving' : 'Save'}</button>
      </form>
      {record ? (
        <div className="row-actions">
          {record.status !== 'PUBLISHED' ? <button className="quiet-button" type="button" onClick={() => void changeStatus('PUBLISHED')} disabled={pending}>Publish</button> : null}
          {record.status !== 'DRAFT' ? <button className="quiet-button" type="button" onClick={() => void changeStatus('DRAFT')} disabled={pending}>Return to draft</button> : null}
        </div>
      ) : null}
    </>
  )
}
