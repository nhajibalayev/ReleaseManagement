import { apiFetch } from './client';

export type ReleaseTaskState =
  | 'Draft'
  | 'PendingInfoSec'
  | 'Security'
  | 'PendingItRisk'
  | 'ItRiskReview'
  | 'PendingFinalApproval'
  | 'Approved'
  | 'Rejected'
  | 'Released';

export interface ReleaseTask {
  id: string;
  title: string;
  scopeDescription: string;
  state: ReleaseTaskState;
  adoWorkItemId?: number;
  createdBy: string;
  createdAt: string;
}

export interface CreateReleaseTaskRequest {
  title: string;
  scopeDescription: string;
}

export function listReleaseTasks() {
  return apiFetch<ReleaseTask[]>('/api/release-tasks');
}

export function getReleaseTask(id: string) {
  return apiFetch<ReleaseTask>(`/api/release-tasks/${id}`);
}

export function createReleaseTask(payload: CreateReleaseTaskRequest) {
  return apiFetch<ReleaseTask>('/api/release-tasks', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}
