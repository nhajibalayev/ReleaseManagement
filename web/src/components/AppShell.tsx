import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export default function AppShell() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const onLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="shell">
      <header className="shell-header">
        <div className="brand">
          <Link to="/release-tasks">ReleaseHub</Link>
        </div>
        <nav>
          <NavLink to="/release-tasks">Release Tasks</NavLink>
          <NavLink to="/release-tasks/new">New</NavLink>
        </nav>
        <div>
          <span style={{ marginRight: 12, color: '#6b7280' }}>{user?.displayName}</span>
          <button onClick={onLogout}>Logout</button>
        </div>
      </header>
      <main className="shell-main">
        <Outlet />
      </main>
    </div>
  );
}
