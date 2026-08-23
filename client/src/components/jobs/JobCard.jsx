import { Link } from 'react-router-dom'

function MatchScore({ score }) {
  if (score == null) {
    return <span className="font-mono text-xs text-nexus-muted">—</span>
  }

  const color =
    score > 80
      ? 'text-nexus-success'
      : score >= 50
        ? 'text-nexus-amber'
        : 'text-nexus-muted'

  return (
    <span className={`font-mono text-xs font-medium ${color}`}>{score}%</span>
  )
}

function JobCard({ job, compact = false }) {
  return (
    <div className="bg-nexus-card border border-nexus-border border-l-2 border-l-nexus-amber rounded-lg p-4 hover:border-nexus-muted transition-colors duration-200">
      <div className="flex items-start justify-between gap-4">
        <div className="min-w-0">
          <h3
            className={`font-medium text-nexus-text truncate ${
              compact ? 'text-sm' : 'text-base'
            }`}
          >
            {job.title}
          </h3>
          <p className={`text-nexus-muted mt-0.5 ${compact ? 'text-xs' : 'text-sm'}`}>
            {job.company}
          </p>
          {job.location && (
            <p className="text-xs text-nexus-dim mt-1">{job.location}</p>
          )}
        </div>

        <div className="flex items-center gap-2 flex-shrink-0">
          {job.isRemote && (
            <span className="text-[10px] px-2 py-0.5 rounded-full bg-nexus-success/10 text-nexus-success">
              Remote
            </span>
          )}
          {job.source && (
            <span className="text-[10px] px-2 py-0.5 rounded-full bg-nexus-muted/10 text-nexus-muted">
              {job.source}
            </span>
          )}
          <MatchScore score={job.matchedScore} />
        </div>
      </div>

      <div className="mt-3 flex items-center gap-4">
        <Link
          to={`/jobs/${job.id}/suggestions`}
          className="inline-flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-lg border border-nexus-amber text-nexus-amber hover:bg-nexus-amber hover:text-nexus-amber-dark transition-colors duration-200"
        >
          <svg
            className="w-3.5 h-3.5"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <path d="M12 3l1.912 5.813a2 2 0 001.275 1.275L21 12l-5.813 1.912a2 2 0 00-1.275 1.275L12 21l-1.912-5.813a2 2 0 00-1.275-1.275L3 12l5.813-1.912a2 2 0 001.275-1.275L12 3z" />
          </svg>
          Suggestions
        </Link>

        {job.sourceUrl && (
          <a
            href={job.sourceUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1 text-xs font-medium text-nexus-amber hover:text-nexus-dim transition-colors"
          >
            <svg
              className="w-3.5 h-3.5"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={2}
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"
              />
            </svg>
            View Original Posting
          </a>
        )}
      </div>
    </div>
  )
}

export default JobCard