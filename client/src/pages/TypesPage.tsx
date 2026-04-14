import { FormEvent, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, DocumentTypeDto } from '../api';

export default function TypesPage() {
  const { t } = useTranslation();
  const [types, setTypes] = useState<DocumentTypeDto[]>([]);
  const [name, setName] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [error, setError] = useState<string | null>(null);

  const load = () => api.get<DocumentTypeDto[]>('/document-types').then((r) => setTypes(r.data));
  useEffect(() => { load(); }, []);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await api.post('/document-types', { name, displayName });
      setName(''); setDisplayName('');
      load();
    } catch (err: any) {
      setError(err.response?.data?.error || t('errors.generic'));
    }
  };

  return (
    <>
      <div className="card">
        <h2>{t('types.title')}</h2>
        <table>
          <thead>
            <tr>
              <th>{t('types.name')}</th>
              <th>{t('types.displayName')}</th>
              <th>{t('types.builtIn')}</th>
            </tr>
          </thead>
          <tbody>
            {types.map((tp) => (
              <tr key={tp.id}>
                <td>{tp.name}</td>
                <td>{tp.displayName}</td>
                <td>{tp.isBuiltIn ? '✓' : ''}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="card">
        <h3>{t('types.create')}</h3>
        <form onSubmit={submit}>
          <label>{t('types.name')}</label>
          <input value={name} onChange={(e) => setName(e.target.value)} required />
          <label>{t('types.displayName')}</label>
          <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
          {error && <div className="error">{error}</div>}
          <button className="btn" type="submit" style={{ marginTop: 12 }}>{t('types.submit')}</button>
        </form>
      </div>
    </>
  );
}
