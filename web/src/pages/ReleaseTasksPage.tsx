import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { listReleaseTasks, ReleaseTask, ReleaseTaskState } from '../api/releaseTasks';
import { ApiError } from '../api/client';

function stateBadgeClass(state: ReleaseTaskState): string {
  switch (state) {
    case 'Draft': return 'badge draft';
    case 'PendingInfoSec':
    case 'PendingItRisk':
    case 'PendingFinalApproval': return 'badge pending';
    case 'Security':
    case 'ItRiskReview': return 'badge security';
    case 'Approved':
    case 'Released': return 'badge approved';
    case 'Rejected': return 'badge rejected';
    default: return 'badge';
  }
}

export default function ReleaseTasksPage() {
  const [tasks, setTasks] = useState<ReleaseTask[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    listReleaseTasks()
      .then(setTasks)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load'))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <h2 style={{ margin: 0 }}>Release Tasks</h2>
        <Link to="/release-tasks/new"><button className="primary">Create release task</button></Link>
      </div>
      <div className="card" style={{ padding: 0 }}>
        {loading && <div style={{ padding: 16 }}>Loading...</div>}
        {error && <div className="error" style={{ padding: 16 }}>{error}</div>}
        {!loading && !error && tasks.length === 0 && (
          <div style={{ padding: 16, color: '#6b7280' }}>No release tasks yet.</div>
        )}
        {!loading && !error && tasks.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Title</th>
                <th>State</th>
                <th>ADO #</th>
                <th>Created by</th>
                <th>Created at</th>
              </tr>
            </thead>
            <tbody>
              {tasks.map((t) => (
                <tr key={t.id}>
                  <td><Link to={'/release-tasks/' + t.id}>{t.title}</Link></td>
                  <td><span className={stateBadgeClass(t.state)}>{t.state}</span></td>
                  <td>{t.adoWorkItemId ?? '-'}</td>
                  <td>{t.createdBy}</td>
                  <td>{new Date(t.createdAt).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
