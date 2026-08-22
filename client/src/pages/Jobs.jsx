import { useState, useEffect } from 'react'
import { getAllJobs } from '../services/jobsApi'
import { createAgentStatusConnection } from '../services/signalrConnection'
import JobCard from '../components/jobs/JobCard'

function Jobs() {
  const [jobs, setJobs] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    getAllJobs()
      .then((data) => setJobs(data))
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    const connection = createAgentStatusConnection()

    connection.on('AgentStatus', (status) => {
      if (status.state === 'Completed') {
        getAllJobs()
          .then((data) => setJobs(data))
          .catch((err) => setError(err.message))
      }
    })

    connection.start().catch((err) => console.error('SignalR connection error:', err))

    return () => {
      connection.stop()
    }
  }, [])

  const validJobs = jobs.filter((job) => job.title && job.company)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-medium text-nexus-text">Jobs</h2>
          <p className="text-xs text-nexus-dim">
            {validJobs.length} {validJobs.length === 1 ? 'job' : 'jobs'} tracked
          </p>
        </div>
      </div>

      {loading && (
        <div className="flex items-center justify-center py-24">
          <div className="w-8 h-8 border-2 border-nexus-border border-t-nexus-amber rounded-full animate-spin" />
        </div>
      )}

      {error && (
        <div className="bg-nexus-danger/10 border border-nexus-danger/30 text-nexus-danger rounded-lg p-4 text-sm">
          Error: {error}
        </div>
      )}

      {!loading && !error && validJobs.length === 0 && (
        <div className="flex flex-col items-center justify-center py-24 text-center bg-nexus-card border border-nexus-border rounded-lg">
          <p className="text-nexus-muted font-medium mb-1">Koi job abhi nahi mila</p>
          <p className="text-xs text-nexus-dim">
            Discovery Agent apne aap jobs dhoondh kar yahan dikhayega.
          </p>
        </div>
      )}

      {!loading && !error && validJobs.length > 0 && (
        <div className="space-y-3">
          {validJobs.map((job) => (
            <JobCard key={job.id} job={job} />
          ))}
        </div>
      )}
    </div>
  )
}

export default Jobs