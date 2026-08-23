import { BrowserRouter, Routes, Route, useLocation } from 'react-router-dom'
import { AnimatePresence } from 'framer-motion'
import AppLayout from './components/layout/AppLayout'
import PageTransition from './components/layout/PageTransition'
import Dashboard from './pages/Dashboard'
import Jobs from './pages/Jobs'
import JobSuggestions from './pages/JobSuggestions'
import Agents from './pages/Agents'
import Analytics from './pages/Analytics'

function AnimatedRoutes() {
  const location = useLocation()

  return (
    <AnimatePresence mode="wait">
      <Routes location={location} key={location.pathname}>
        <Route element={<AppLayout />}>
          <Route
            index
            element={
              <PageTransition>
                <Dashboard />
              </PageTransition>
            }
          />
          <Route
            path="jobs"
            element={
              <PageTransition>
                <Jobs />
              </PageTransition>
            }
          />
          <Route
            path="jobs/:id/suggestions"
            element={
              <PageTransition>
                <JobSuggestions />
              </PageTransition>
            }
          />
          <Route
            path="agents"
            element={
              <PageTransition>
                <Agents />
              </PageTransition>
            }
          />
          <Route
            path="analytics"
            element={
              <PageTransition>
                <Analytics />
              </PageTransition>
            }
          />
        </Route>
      </Routes>
    </AnimatePresence>
  )
}

function App() {
  return (
    <BrowserRouter>
      <AnimatedRoutes />
    </BrowserRouter>
  )
}

export default App