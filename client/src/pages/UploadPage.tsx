import { FormEvent, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, DocumentDto, DocumentTypeDto } from '../api';

export default function UploadPage() {
  const { t } = useTranslation();
  const [types, setTypes] = useState<DocumentTypeDto[]>([]);
  const [availableRoles, setAvailableRoles] = useState<string[]>([]);
  const [selectedRoles, setSelectedRoles] = useState<string[]>(['admin']);
  const [file, setFile] = useState<File | null>(null);
  const [type, setType] = useState('');
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<DocumentDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<DocumentTypeDto[]>('/document-types').then((r) => {
      setTypes(r.data);
      if (r.data.length > 0) setType(r.data[0].name);
    });
    api.get<string[]>('/roles').then((r) => {
      const merged = Array.from(new Set([...r.data, 'admin', 'user']));
      setAvailableRoles(merged);
    });
  }, []);

  const toggleRole = (role: string) => {
    setSelectedRoles((prev) =>
      prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role]
    );
  };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!file) return;
    if (selectedRoles.length === 0) {
      setError(t('upload.selectAtLeastOneRole'));
      return;
    }
    setBusy(true);
    setError(null);
    setResult(null);
    try {
      const fd = new FormData();
      fd.append('file', file);
      fd.append('type', type);
      fd.append('allowedRoles', selectedRoles.join(','));
      const { data } = await api.post<DocumentDto>('/documents/upload', fd);
      setResult(data);
    } catch (err: any) {
      setError(err.response?.data?.error || t('errors.generic'));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="card">
      <h2>{t('upload.title')}</h2>
      <form onSubmit={submit}>
        <label>{t('upload.file')}</label>
        <input type="file" onChange={(e) => setFile(e.target.files?.[0] ?? null)} required />

        <label>{t('upload.type')}</label>
        <select value={type} onChange={(e) => setType(e.target.value)}>
          {types.map((tp) => (
            <option key={tp.id} value={tp.name}>{tp.displayName}</option>
          ))}
        </select>

        <label>{t('upload.allowedRoles')}</label>
        <div className="chips">
          {availableRoles.map((r) => (
            <span
              key={r}
              className={`chip ${selectedRoles.includes(r) ? 'selected' : ''}`}
              onClick={() => toggleRole(r)}
            >
              {selectedRoles.includes(r) ? '✓ ' : ''}{r}
            </span>
          ))}
        </div>

        {error && <div className="error">{error}</div>}
        {result && <div className="success">{t('upload.success', { version: result.version })}</div>}
        <button className="btn" type="submit" disabled={busy || !file} style={{ marginTop: 12 }}>
          {busy ? t('upload.uploading') : t('upload.submit')}
        </button>
      </form>
    </div>
  );
}
