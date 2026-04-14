import { FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../auth';

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [email, setEmail] = useState('admin@gmail.com');
  const [password, setPassword] = useState('12345678Aa');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await login(email, password);
      navigate('/documents');
    } catch {
      setError(t('login.error'));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="container" style={{ maxWidth: 420, marginTop: 80 }}>
      <div className="card">
        <h2>{t('login.title')}</h2>
        <form onSubmit={submit}>
          <label>{t('login.email')}</label>
          <input value={email} onChange={(e) => setEmail(e.target.value)} type="email" required />
          <label>{t('login.password')}</label>
          <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" required />
          {error && <div className="error">{error}</div>}
          <button className="btn" type="submit" disabled={busy} style={{ marginTop: 12 }}>
            {t('login.submit')}
          </button>
        </form>
      </div>
    </div>
  );
}
