import { useState, useEffect, useRef } from 'react'
import { motion, useInView, animate } from 'framer-motion'
import { getAllJobs } from '../services/jobsApi'
import { createAgentStatusConnection } from '../services/signalrConnection'
import { runFullCycle } from '../services/orchestratorApi'
import JobCard from '../components/jobs/JobCard'

function CountUp({ value, duration = 800 }) {
  const ref = useRef(null)
  const inView = useInView(ref, { once: true })
  const [display, setDisplay] = useState(0)

  useEffect(() => {
    if (!inView) return
    const controls = animate(0, value, {
      duration: duration / 1000,
      ease: 'easeOut',
      onUpdate: (v) => setDisplay(Math.round(v)),
    })
    return () => controls.stop()
  }, [inView, value, duration])

  return <span ref={ref}>{display}</span>
}

function StatCard({ label, value, unit = '', color = 'text-nexus-text', animateCount = true }) {
  return (
    <div className="bg-nexus-card border border-nexus-border rounded-lg p-4">
      <p className="text-[11px] text-nexus-dim">{label}</p>
      <p className={`mt-1 font-mono font-medium text-[22px] leading-none ${color}`}>
        {animateCount && typeof value === 'number' ? (
          <>
            <CountUp value={value} />
            {unit}
          </>
        ) : (
          <>
            {value}
            {unit}
          </>
        )}
      </p>
    </div>
  )
}

function Dashboard() {
  const [jobs, setJobs] = useState([])
  const [loading, setLoading] = useState(true)
  const [agentStatus, setAgentStatus] = useState(null)
  const [running, setRunning] = useState(false)
  const [runError, setRunError] = useState(null)

  useEffect(() => {
    getAllJobs()
      .then((data) => setJobs(data))
      .catch((err) => console.error('Failed to fetch jobs:', err))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    const connection = createAgentStatusConnection()

    connection.on('AgentStatus', (status) => {
      setAgentStatus(status)

      if (status.state === 'Completed') {
        getAllJobs().then((data) => setJobs(data))
      }
    })

    connection.start().catch((err) => console.error('SignalR connection error:', err))

    return () => {
      connection.stop()
    }
  }, [])

  const validJobs = jobs.filter((job) => job.title && job.company)

  const totalJobs = validJobs.length
  const scores = validJobs
    .map((j) => j.matchedScore)
    .filter((s) => s != null)
  const avgScore = scores.length
    ? Math.round(scores.reduce((a, b) => a + b, 0) / scores.length)
    : null

  const agentRunning =
    agentStatus &&
    (agentStatus.state === 'Started' || agentStatus.state === 'Progress')

  const recentJobs = [...validJobs].slice(0, 4)

  const handleRunFullCycle = async () => {
    setRunning(true)
    setRunError(null)
    try {
      await runFullCycle()
    } catch (err) {
      setRunError(err.message)
    } finally {
      setRunning(false)
    }
  }

  return (
    <div className="space-y-6">
      {/* Header row */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-medium text-nexus-text">Dashboard</h2>
          <p className="text-xs text-nexus-dim">Mission overview</p>
        </div>
        <button
          onClick={handleRunFullCycle}
          disabled={running}
          className={`flex items-center gap-2 text-xs font-medium px-3 py-1.5 rounded-lg border transition-colors duration-200 ${
            running
              ? 'border-nexus-border text-nexus-dim cursor-wait'
              : 'border-nexus-amber text-nexus-amber hover:bg-nexus-amber hover:text-nexus-amber-dark'
          }`}
        >
          <span
            className={`w-2 h-2 rounded-full ${
              running ? 'bg-nexus-amber animate-pulse' : 'bg-nexus-amber'
            }`}
          />
          {running ? 'Running...' : 'Run Full Cycle'}
        </button>
      </div>

      {runError && (
        <div className="bg-nexus-danger/10 border border-nexus-danger/30 text-nexus-danger rounded-lg px-4 py-2 text-xs">
          {runError}
        </div>
      )}

      {/* Stat cards */}
      <div className="grid grid-cols-3 gap-3">
        <StatCard label="jobs tracked" value={totalJobs} />
        <StatCard
          label="avg match score"
          value={avgScore != null ? avgScore : '—'}
          unit={avgScore != null ? '%' : ''}
          color="text-nexus-success"
          animateCount={avgScore != null}
        />
        <StatCard
          label="agents active"
          value={agentRunning ? '1 / 4' : '0 / 4'}
          color={agentRunning ? 'text-nexus-amber' : 'text-nexus-text'}
          animateCount={false}
        />
      </div>

      {/* Agent status strip */}
      {agentStatus && (
        <div className="flex items-center gap-2.5 bg-nexus-card border border-nexus-border rounded-lg px-4 py-3">
          <motion.span
            className="w-2 h-2 rounded-full bg-nexus-amber"
            animate={{ scale: [1, 1.4, 1] }}
            transition={{ repeat: Infinity, duration: 1.2, ease: 'easeInOut' }}
          />
          <p className="text-sm text-nexus-muted">
            {agentStatus.state === 'Completed' ? (
              <>
                <strong className="text-nexus-success">
                  {agentStatus.agentType} Agent
                </strong>{' '}
                finished
              </>
            ) : (
              <>
                <strong className="text-nexus-amber">
                  {agentStatus.agentType} Agent
                </strong>{' '}
                {agentStatus.message}
              </>
            )}
          </p>
        </div>
      )}

      {/* Recent jobs */}
      <div>
        <h3 className="text-xs text-nexus-dim mb-3">recent jobs</h3>

        {loading && (
          <div className="flex items-center justify-center py-16">
            <div className="w-8 h-8 border-2 border-nexus-border border-t-nexus-amber rounded-full animate-spin" />
          </div>
        )}

        {!loading && recentJobs.length === 0 && (
          <div className="flex flex-col items-center justify-center py-16 text-center bg-nexus-card border border-nexus-border rounded-lg">
            <p className="text-nexus-muted font-medium mb-1">Koi job abhi nahi mila</p>
            <p className="text-xs text-nexus-dim">
              Discovery Agent apne aap jobs dhoondh kar yahan dikhayega.
            </p>
          </div>
        )}

        {!loading && recentJobs.length > 0 && (
          <div className="space-y-2.5">
            {recentJobs.map((job) => (
              <JobCard key={job.id} job={job} compact />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

export default Dashboard