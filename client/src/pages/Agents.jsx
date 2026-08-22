import { useState, useEffect } from 'react'
import { getDashboardStats } from '../services/analyticsApi'

const statusColors = {
  Success: 'text-nexus-success',
  Failed: 'text-nexus-danger',
  Partial: 'text-nexus-amber',
}

const statusDots = {
  Success: 'bg-nexus-success',
  Failed: 'bg-nexus-danger',
  Partial: 'bg-nexus-amber',
}

function formatTime(iso) {
  return new Date(iso).toLocaleString()
}

function Agents() {
  const [runs, setRuns] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    getDashboardStats()
      .then((stats) => setRuns(stats.recentAgentRuns || []))
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-medium text-nexus-text">Agent activity</h2>
        <p className="text-xs text-nexus-dim">Recent agent run history</p>
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

      {!loading && !error && runs.length === 0 && (
        <div className="flex flex-col items-center justify-center py-24 text-center bg-nexus-card border border-nexus-border rounded-lg">
          <p className="text-nexus-muted font-medium">Coming soon</p>
          <p className="text-xs text-nexus-dim mt-1">
            Koi recent agent run abhi tak record nahi hua.
          </p>
        </div>
      )}

      {!loading && !error && runs.length > 0 && (
        <div className="bg-nexus-card border border-nexus-border rounded-lg divide-y divide-nexus-border">
          {runs.map((run, i) => (
            <div key={i} className="flex items-center gap-3 px-4 py-3">
              <span className={`w-2 h-2 rounded-full flex-shrink-0 ${statusDots[run.status] || 'bg-nexus-dim'}`} />
              <div className="min-w-0 flex-1">
                <p className="text-sm font-medium text-nexus-text">
                  {run.agentType} Agent
                </p>
                <p className="text-xs text-nexus-dim">
                  {formatTime(run.runAt)}
                  {run.durationMs != null && ` · ${run.durationMs}ms`}
                </p>
              </div>
              <span className={`text-xs font-medium ${statusColors[run.status] || 'text-nexus-muted'}`}>
                {run.status}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export default Agents