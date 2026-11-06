import { useEffect, useState, type FormEvent } from 'react'
import { SiteLink } from '../../shared/SiteLink.tsx'
import {
  fetchReadingQuest,
  fetchReadingQuests,
  submitAnswer,
  type ReadingQuest,
  type ReadingQuestSummary,
} from '../quests/quest-api.ts'

export function ParticipantQuests() {
  const [quests, setQuests] = useState<ReadingQuestSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    fetchReadingQuests()
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

  if (error) return <p className="section-note" role="alert">{error}</p>
  if (!quests) return <p className="section-note">Opening the tasks…</p>

  return (
    <>
      <p className="eyebrow">Tasks</p>
      <h1>Tasks</h1>
      <ParticipantQuestList quests={quests} />
    </>
  )
}

export function ParticipantQuestList({ quests }: { quests: ReadingQuestSummary[] }) {
  const open = quests.filter((item) => item.openTasks > 0)
  if (open.length === 0) return <p className="section-note">Nothing is waiting for you.</p>

  return (
    <div className="task-stack">
      {open.map((item) => (
        <section key={item.id}>
          <p className="status-mark">{item.reference}</p>
          <h2>
            <SiteLink href={`/client/tasks/${item.id}`} className="text-link">{item.title}</SiteLink>
          </h2>
          <p>{item.description}</p>
          <p className="section-note">{item.completedTasks} completed, {item.openTasks} open</p>
        </section>
      ))}
    </div>
  )
}

export function ParticipantQuest({ questId }: { questId: string }) {
  const [quest, setQuest] = useState<ReadingQuest | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)
  const [notices, setNotices] = useState<Record<string, string>>({})

  useEffect(() => {
    let active = true
    fetchReadingQuest(questId)
      .then((item) => {
        if (active) setQuest(item)
      })
      .catch((caught: unknown) => {
        if (active) setError(caught instanceof Error ? caught.message : 'That quest is not available.')
      })
    return () => {
      active = false
    }
  }, [questId])

  async function send(taskId: string, answer: string) {
    if (pending) return
    setPending(true)
    setError(null)
    try {
      const result = await submitAnswer(taskId, answer)
      setNotices((current) => ({ ...current, [taskId]: result.message }))
      setQuest(await fetchReadingQuest(questId))
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The answer could not be submitted.')
    } finally {
      setPending(false)
    }
  }

  if (error && !quest) return <p className="section-note" role="alert">{error}</p>
  if (!quest) return <p className="section-note">Opening the task…</p>

  return <QuestReading quest={quest} pending={pending} notices={notices} error={error} onSubmit={send} />
}

export function QuestReading({
  quest,
  pending = false,
  notices = {},
  error = null,
  onSubmit,
}: {
  quest: ReadingQuest
  pending?: boolean
  notices?: Record<string, string>
  error?: string | null
  onSubmit?: (taskId: string, answer: string) => void
}) {
  return (
    <>
      <p className="eyebrow">{quest.reference}</p>
      <h1>{quest.title}</h1>
      <p className="section-note">{quest.description}</p>
      <p className="paper">{quest.instructions}</p>
      {quest.caseId ? (
        <p className="section-note">
          <SiteLink href={`/client/cases/${quest.caseId}`} className="text-link">{quest.caseTitle || quest.caseReference}</SiteLink>
        </p>
      ) : null}
      <p className="section-note">Shared progress: {quest.sharedCompleted} completed, {quest.sharedOpen} open.</p>
      {error ? <p className="form-status" role="alert">{error}</p> : null}
      {quest.tasks.filter((task) => task.completed).map((task) => {
        const note = notices[task.id] || task.successMessage
        return note ? <p key={task.id} role="status">{note}</p> : null
      })}
      {quest.tasks.some((task) => !task.completed) ? (
      <div className="task-stack">
        {quest.tasks.filter((task) => !task.completed).map((task) => (
          <section key={task.id}>
            <p className="status-mark">{task.reference}</p>
            <h2>{task.title}</h2>
            <p className="paper">{task.instructions}</p>
            {task.shared ? <p className="section-note">Held for both account holders.</p> : null}
            <p className="section-note">Open</p>
            {notices[task.id] ? <p role="status">{notices[task.id]}</p> : null}
            {onSubmit ? (
              <form
                className="enquiry"
                onSubmit={(event: FormEvent<HTMLFormElement>) => {
                  event.preventDefault()
                  const data = new FormData(event.currentTarget)
                  onSubmit(task.id, String(data.get('answer') ?? ''))
                  event.currentTarget.reset()
                }}
              >
                <label>
                  Answer
                  <input name="answer" type="text" autoComplete="off" required />
                </label>
                <button type="submit" disabled={pending}>{pending ? 'Submitting' : 'Submit'}</button>
              </form>
            ) : null}
          </section>
        ))}
      </div>
      ) : null}
    </>
  )
}
