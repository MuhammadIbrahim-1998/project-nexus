import { NavLink, Outlet } from 'react-router-dom'
import { GridIcon, BriefcaseIcon, BotIcon, ChartIcon } from '../Icons'

const navItems = [
  { to: '/', label: 'Dashboard', icon: GridIcon, end: true },
  { to: '/jobs', label: 'Jobs', icon: BriefcaseIcon },
  { to: '/agents', label: 'Agents', icon: BotIcon },
  { to: '/analytics', label: 'Analytics', icon: ChartIcon },
]

function AppLayout() {
  return (
    <div className="min-h-screen bg-nexus-bg text-nexus-text flex">
      {/* Left sidebar */}
      <aside className="w-16 flex-shrink-0 bg-nexus-sidebar border-r border-nexus-border flex flex-col items-center py-4">
        {/* Logo mark */}
        <div className="w-8 h-8 rounded bg-nexus-amber flex items-center justify-center mb-8">
          <span className="font-bold text-sm text-nexus-amber-dark">N</span>
        </div>

        {/* Nav icons */}
        <nav className="flex flex-col gap-1 flex-1">
          {navItems.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              title={label}
              className="group relative flex items-center justify-center w-10 h-10 rounded-lg transition-colors duration-200"
            >
              {({ isActive }) => (
                <>
                  {isActive && (
                    <span className="absolute left-0 top-1/2 -translate-y-1/2 w-0.5 h-5 rounded-full bg-nexus-amber" />
                  )}
                  <Icon
                    className={`transition-colors duration-200 ${
                      isActive
                        ? 'text-nexus-amber'
                        : 'text-nexus-dim group-hover:text-nexus-muted'
                    }`}
                  />
                </>
              )}
            </NavLink>
          ))}
        </nav>
      </aside>

      {/* Right: header + content */}
      <div className="flex-1 flex flex-col min-w-0">
        <header className="border-b border-nexus-border px-6 py-4">
          <h1 className="text-base font-medium text-nexus-text">Mission Control</h1>
          <p className="text-xs text-nexus-dim mt-0.5">Project Nexus</p>
        </header>

        <main className="flex-1 p-6 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

export default AppLayout