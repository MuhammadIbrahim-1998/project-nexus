const API_BASE_URL = 'http://localhost:5235/api'

export async function getAllJobs() {
  const response = await fetch(`${API_BASE_URL}/Jobs`)
  if (!response.ok) {
    throw new Error('Failed to fetch jobs')
  }
  return response.json()
}
