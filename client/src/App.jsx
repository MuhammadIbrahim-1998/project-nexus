import { useState, useEffect } from 'react'
import { getAllJobs } from './services/jobsApi'
import { createAgentStatusConnection } from './services/signalrConnection'
import './App.css'

function App() {
  const [jobs, setJobs] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [agentStatus, setAgentStatus] = useState(null)

  useEffect(() => {
    getAllJobs()
      .then((data) => setJobs(data))
      .catch((err) => setError(err.message))
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

  const bannerStyles = {
    Started: 'bg-blue-50 border-blue-200 text-blue-800',
    Progress: 'bg-blue-50 border-blue-200 text-blue-800',
    Completed: 'bg-green-50 border-green-200 text-green-800',
    Failed: 'bg-red-50 border-red-200 text-red-800',
  }

  const dotStyles = {
    Started: 'bg-blue-500 animate-pulse',
    Progress: 'bg-blue-500 animate-pulse',
    Completed: 'bg-green-500',
    Failed: 'bg-red-500',
  }

  return (
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white border-b border-gray-200 px-8 py-5 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Project Nexus</h1>
          <p className="text-sm text-gray-500">Multi-Agent Job Search Dashboard</p>
        </div>
        <div className="bg-blue-50 text-blue-700 text-sm font-medium px-3 py-1.5 rounded-full">
          {validJobs.length} {validJobs.length === 1 ? 'job' : 'jobs'} tracked
        </div>
      </header>

      <main className="p-8">
        {agentStatus && (
          <div
            className={`flex items-center gap-2.5 border rounded-lg p-3 mb-6 text-sm ${
              bannerStyles[agentStatus.state] || bannerStyles.Started
            }`}
          >
            <span
              className={`w-2 h-2 rounded-full flex-shrink-0 ${
                dotStyles[agentStatus.state] || dotStyles.Started
              }`}
            />
            <span>
              <strong>{agentStatus.agentType} Agent:</strong> {agentStatus.message}
            </span>
          </div>
        )}

        {loading && (
          <div className="flex flex-col items-center justify-center py-20">
            <div className="w-10 h-10 border-4 border-blue-200 border-t-blue-600 rounded-full animate-spin mb-4" />
            <p className="text-gray-500 text-sm">Jobs load ho rahi hain...</p>
          </div>
        )}

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg p-4 text-sm">
            Error: {error}
          </div>
        )}

        {!loading && !error && validJobs.length === 0 && (
          <div className="flex flex-col items-center justify-center py-20 text-center">
            <div className="text-5xl mb-3">🔍</div>
            <p className="text-gray-700 font-medium mb-1">Koi job abhi nahi mila</p>
            <p className="text-gray-500 text-sm">Discovery Agent apne aap jobs dhoondh kar yahan dikhayega.</p>
          </div>
        )}

        <div className="space-y-4">
          {validJobs.map((job) => (
            <div
              key={job.id}
              className="bg-white rounded-lg shadow p-5 border-l-4 border-blue-500 hover:shadow-md transition-shadow"
            >
              <div className="flex items-start justify-between">
                <div>
                  <h2 className="text-xl font-semibold text-gray-900">{job.title}</h2>
                  <p className="text-gray-600">{job.company}</p>
                </div>

                <div className="flex gap-2 flex-shrink-0">
                  {job.isRemote && (
                    <span className="bg-green-50 text-green-700 text-xs font-medium px-2.5 py-1 rounded-full">
                      Remote
                    </span>
                  )}
                  {job.source && (
                    <span className="bg-gray-100 text-gray-600 text-xs font-medium px-2.5 py-1 rounded-full">
                      {job.source}
                    </span>
                  )}
                </div>
              </div>

              {job.location && (
                <p className="text-sm text-gray-500 mt-2">📍 {job.location}</p>
              )}
            </div>
          ))}
        </div>
      </main>
    </div>
  )
}

export default App
