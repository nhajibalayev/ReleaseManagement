import { FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createReleaseTask } from '../api/releaseTasks';
import { ApiError } from '../api/client';

export default function CreateReleaseTaskPage() {
  const navigate = useNavigate();
  const [title, setTitle] = useState('');
  const [scope, setScope] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const created = await createReleaseTask({ title, scopeDescription: scope });
      navigate('/release-tasks/' + created.id, { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to create release task');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <h2>Create release task</h2>
      <form className="card" onSubmit={onSubmit}>
        <div className="field">
          <label htmlFor="title">Title</label>
          <input
            id="title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="e.g. June 2026 production release"
            required
          />
        </div>
        <div className="field">
          <label htmlFor="scope">Scope &mdash; which projects/components are being released, with versions</label>
          <textarea
            id="scope"
            value={scope}
            onChange={(e) => setScope(e.target.value)}
            rows={8}
            placeholder="Describe each project, version, change summary..."
            required
          />
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className="primary" type="submit" disabled={loading}>
            {loading ? 'Creating...' : 'Create'}
          </button>
          <button type="button" onClick={() => navigate(-1)} disabled={loading}>Cancel</button>
        </div>
        {error && <div className="error">{error}</div>}
      </form>
    </div>
  );
}
