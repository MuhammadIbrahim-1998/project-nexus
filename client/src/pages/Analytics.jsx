import { useState, useEffect } from 'react'
import { getDashboardStats } from '../services/analyticsApi'

function StatCard({ label, value, color = 'text-nexus-text' }) {
  return (
    <div className="bg-nexus-card border border-nexus-border rounded-lg p-4">
      <p className="text-[11px] text-nexus-dim">{label}</p>
      <p className={`mt-1 font-mono font-medium text-[22px] leading-none ${color}`}>
        {value}
      </p>
    </div>
  )
}

function Analytics() {
  const [stats, setStats] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    getDashboardStats()
      .then((data) => setStats(data))
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-medium text-nexus-text">Analytics</h2>
        <p className="text-xs text-nexus-dim">Pipeline overview</p>
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

      {!loading && !error && !stats && (
        <div className="flex flex-col items-center justify-center py-24 text-center bg-nexus-card border border-nexus-border rounded-lg">
          <p className="text-nexus-muted font-medium">Coming soon</p>
          <p className="text-xs text-nexus-dim mt-1">
            Analytics data abhi available nahi hai.
          </p>
        </div>
      )}

      {!loading && !error && stats && (
        <>
          <div className="grid grid-cols-3 gap-3">
            <StatCard label="total jobs discovered" value={stats.totalJobsDiscovered} />
            <StatCard
              label="avg match score"
              value={stats.averageMatchScore != null ? `${Math.round(stats.averageMatchScore)}%` : '—'}
              color="text-nexus-success"
            />
            <StatCard
              label="content generated"
              value={stats.totalContentGenerated}
              color="text-nexus-amber"
            />
          </div>

          <div className="grid grid-cols-3 gap-3">
            <StatCard label="high match (80%+)" value={stats.highMatchCount} color="text-nexus-success" />
            <StatCard label="medium match (50-80%)" value={stats.mediumMatchCount} color="text-nexus-amber" />
            <StatCard label="low match (<50%)" value={stats.lowMatchCount} color="text-nexus-muted" />
          </div>
        </>
      )}
    </div>
  )
}

export default Analytics