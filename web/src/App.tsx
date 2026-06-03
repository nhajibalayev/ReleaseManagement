import { Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import LoginPage from './pages/LoginPage';
import ReleaseTasksPage from './pages/ReleaseTasksPage';
import ReleaseTaskDetailsPage from './pages/ReleaseTaskDetailsPage';
import CreateReleaseTaskPage from './pages/CreateReleaseTaskPage';
import AppShell from './components/AppShell';

function RequireAuth({ children }: { children: JSX.Element }) {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  return children;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <RequireAuth>
            <AppShell />
          </RequireAuth>
        }
      >
        <Route index element={<Navigate to="/release-tasks" replace />} />
        <Route path="release-tasks" element={<ReleaseTasksPage />} />
        <Route path="release-tasks/new" element={<CreateReleaseTaskPage />} />
        <Route path="release-tasks/:id" element={<ReleaseTaskDetailsPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
