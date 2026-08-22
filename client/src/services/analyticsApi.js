const API_BASE_URL = 'http://localhost:5235/api'

export async function getDashboardStats() {
  const response = await fetch(`${API_BASE_URL}/Analytics/dashboard-stats`)
  if (!response.ok) {
    throw new Error('Failed to fetch dashboard stats')
  }
  return response.json()
}