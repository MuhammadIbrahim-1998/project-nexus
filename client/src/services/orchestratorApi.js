const API_BASE_URL = 'http://localhost:5235/api'

export async function runFullCycle() {
  const response = await fetch(`${API_BASE_URL}/Orchestrator/run-full-cycle`, {
    method: 'POST',
  })
  if (!response.ok) {
    throw new Error('Failed to start full cycle')
  }
  return response.json()
}