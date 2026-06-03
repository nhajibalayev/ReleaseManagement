import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { loginViaSso } from '../auth/sso';

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const onLogin = async () => {
    setError(null);
    setLoading(true);
    try {
      const user = await loginViaSso();
      login(user);
      navigate('/release-tasks', { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="card login-card" style={{ textAlign: 'center' }}>
        <h2 style={{ marginTop: 0 }}>ReleaseHub</h2>
        <p style={{ color: '#6b7280' }}>
          Sign in with your corporate account.
          <br />
          Your browser will prompt for domain credentials.
        </p>
        <button
          className="primary"
          onClick={onLogin}
          disabled={loading}
          style={{ width: '100%' }}
        >
          {loading ? 'Signing in...' : 'Sign in with SSO'}
        </button>
        {error && <div className="error" style={{ marginTop: 12 }}>{error}</div>}
      </div>
    </div>
  );
}
