const API_BASE_URL = 'http://localhost:5235/api'

export async function getAllJobs() {
  const response = await fetch(`${API_BASE_URL}/Jobs`)
  if (!response.ok) {
    throw new Error('Failed to fetch jobs')
  }
  return response.json()
}

export async function getJobSuggestions(id) {
  const response = await fetch(`${API_BASE_URL}/Jobs/${id}/suggestions`)
  if (response.status === 404) {
    throw new Error('Job not found')
  }
  if (!response.ok) {
    throw new Error('Failed to fetch job suggestions')
  }
  return response.json()
}

