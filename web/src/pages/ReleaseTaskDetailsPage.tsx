import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { getReleaseTask, ReleaseTask } from '../api/releaseTasks';
import { ApiError } from '../api/client';

export default function ReleaseTaskDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const [task, setTask] = useState<ReleaseTask | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    getReleaseTask(id)
      .then(setTask)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load'))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <div>Loading...</div>;
  if (error) return <div className="error">{error}</div>;
  if (!task) return <div>Not found</div>;

  return (
    <div>
      <Link to="/release-tasks">&larr; Back to list</Link>
      <h2 style={{ marginBottom: 4 }}>{task.title}</h2>
      <div style={{ color: '#6b7280', marginBottom: 16 }}>
        State: <span className="badge">{task.state}</span>
        {task.adoWorkItemId && <> &middot; ADO #{task.adoWorkItemId}</>}
      </div>
      <div className="card">
        <h3 style={{ marginTop: 0 }}>Scope</h3>
        <pre style={{ whiteSpace: 'pre-wrap', fontFamily: 'inherit', margin: 0 }}>{task.scopeDescription}</pre>
      </div>
      <div className="card">
        <h3 style={{ marginTop: 0 }}>Metadata</h3>
        <div>Created by: {task.createdBy}</div>
        <div>Created at: {new Date(task.createdAt).toLocaleString()}</div>
      </div>
    </div>
  );
}
