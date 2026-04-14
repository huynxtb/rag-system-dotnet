import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, DocumentDto } from '../api';

export default function DocumentsPage() {
  const { t } = useTranslation();
  const [docs, setDocs] = useState<DocumentDto[]>([]);

  useEffect(() => {
    api.get<DocumentDto[]>('/documents').then((r) => setDocs(r.data));
  }, []);

  return (
    <div className="card">
      <h2>{t('documents.title')}</h2>
      {docs.length === 0 ? (
        <p>{t('documents.empty')}</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>{t('documents.fileName')}</th>
              <th>{t('documents.type')}</th>
              <th>{t('documents.version')}</th>
              <th>{t('documents.status')}</th>
              <th>{t('documents.allowedRoles')}</th>
              <th>{t('documents.uploadedAt')}</th>
            </tr>
          </thead>
          <tbody>
            {docs.map((d) => (
              <tr key={d.id}>
                <td>{d.fileName}</td>
                <td>{d.type}</td>
                <td>v{d.version}</td>
                <td>{d.status}</td>
                <td>{d.allowedRoles.map((r) => <span key={r} className="tag">{r}</span>)}</td>
                <td>{new Date(d.uploadedAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
