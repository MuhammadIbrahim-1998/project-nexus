import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { getJobSuggestions } from '../services/jobsApi'

function Chip({ children, tone = 'muted' }) {
  const className =
    tone === 'amber'
      ? 'bg-nexus-amber-dark/40 text-nexus-amber'
      : 'bg-nexus-border text-nexus-muted'

  return (
    <span className={`text-xs px-2.5 py-1 rounded-full ${className}`}>
      {children}
    </span>
  )
}

function JobSuggestions() {
  const { id } = useParams()
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    getJobSuggestions(id)
      .then((result) => setData(result))
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false))
  }, [id])

  return (
    <div className="space-y-6">
      <div>
        <Link
          to="/jobs"
          className="inline-flex items-center gap-1 text-xs text-nexus-dim hover:text-nexus-muted transition-colors"
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
            <path d="M19 12H5M12 19l-7-7 7-7" />
          </svg>
          Back to Jobs
        </Link>
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

      {!loading && !error && data && (
        <>
          <div>
            <h2 className="text-lg font-medium text-nexus-text">{data.title}</h2>
            <p className="text-sm text-nexus-muted mt-0.5">{data.company}</p>
          </div>

          <div>
            <h3 className="text-xs text-nexus-dim mb-3">missing skills</h3>
            {data.missingSkills.length > 0 ? (
              <div className="flex flex-wrap gap-2">
                {data.missingSkills.map((skill) => (
                  <Chip key={skill} tone="amber">
                    {skill}
                  </Chip>
                ))}
              </div>
            ) : (
              <p className="text-xs text-nexus-dim">
                Koi missing skill record nahi hai.
              </p>
            )}
          </div>

          <div>
            <h3 className="text-xs text-nexus-dim mb-3">suggested projects</h3>
            {data.suggestions.length > 0 ? (
              <div className="space-y-4">
                {data.suggestions.map((suggestion, index) => (
                  <div
                    key={index}
                    className="bg-nexus-card border border-nexus-border rounded-lg p-5"
                  >
                    <div className="flex items-start justify-between gap-4">
                      <h4 className="font-medium text-nexus-text">
                        {suggestion.title}
                      </h4>
                      <button
                        type="button"
                        disabled
                        title="Coming soon"
                        className="flex-shrink-0 text-xs font-medium px-3 py-1.5 rounded-lg border border-nexus-border text-nexus-dim cursor-not-allowed opacity-60"
                      >
                        Implement
                      </button>
                    </div>

                    {suggestion.description && (
                      <p className="text-sm text-nexus-muted mt-2 leading-relaxed">
                        {suggestion.description}
                      </p>
                    )}

                    {suggestion.skillsAddressed.length > 0 && (
                      <div className="flex flex-wrap gap-2 mt-4">
                        {suggestion.skillsAddressed.map((skill) => (
                          <Chip key={skill}>{skill}</Chip>
                        ))}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            ) : (
              <div className="bg-nexus-card border border-nexus-border rounded-lg p-5">
                <p className="text-sm text-nexus-muted">
                  Abhi koi suggestion generate nahi hua.
                </p>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  )
}

export default JobSuggestions