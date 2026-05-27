import { useRef, useState } from 'react';
import ReCAPTCHA from 'react-google-recaptcha';

interface FormState {
  name: string;
  email: string;
  message: string;
}

interface SubmitStatus {
  type: 'success' | 'error';
  message: string;
}

const SITE_KEY = import.meta.env.VITE_RECAPTCHA_SITE_KEY as string;

export default function ContactForm() {
  const [form, setForm] = useState<FormState>({ name: '', email: '', message: '' });
  const [submitting, setSubmitting] = useState(false);
  const [status, setStatus] = useState<SubmitStatus | null>(null);
  const recaptchaRef = useRef<ReCAPTCHA>(null);

  function handleChange(e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) {
    setForm((prev) => ({ ...prev, [e.target.name]: e.target.value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setStatus(null);

    const token = recaptchaRef.current?.getValue();
    if (!token) {
      setStatus({ type: 'error', message: 'Please complete the reCAPTCHA challenge.' });
      return;
    }

    setSubmitting(true);
    try {
      const response = await fetch('/api/contact', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...form, recaptchaToken: token }),
      });

      const data = await response.json();

      if (response.ok) {
        setStatus({ type: 'success', message: data.message });
        setForm({ name: '', email: '', message: '' });
        recaptchaRef.current?.reset();
      } else {
        const errorMsg =
          data.error ??
          Object.values(data.errors ?? {}).flat().join(' ') ??
          'Submission failed. Please try again.';
        setStatus({ type: 'error', message: errorMsg as string });
        recaptchaRef.current?.reset();
      }
    } catch {
      setStatus({ type: 'error', message: 'Network error. Please check your connection and try again.' });
      recaptchaRef.current?.reset();
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="form-container">
      <h1>Contact Us</h1>

      {status && (
        <div className={`alert alert-${status.type}`} role="alert">
          {status.message}
        </div>
      )}

      <form onSubmit={handleSubmit} noValidate>
        <div className="field">
          <label htmlFor="name">Name</label>
          <input
            id="name"
            name="name"
            type="text"
            value={form.name}
            onChange={handleChange}
            placeholder="Your name"
            required
            maxLength={100}
            disabled={submitting}
          />
        </div>

        <div className="field">
          <label htmlFor="email">Email</label>
          <input
            id="email"
            name="email"
            type="email"
            value={form.email}
            onChange={handleChange}
            placeholder="you@example.com"
            required
            maxLength={200}
            disabled={submitting}
          />
        </div>

        <div className="field">
          <label htmlFor="message">Message</label>
          <textarea
            id="message"
            name="message"
            value={form.message}
            onChange={handleChange}
            placeholder="Your message…"
            required
            maxLength={2000}
            rows={5}
            disabled={submitting}
          />
        </div>

        <div className="field">
          <ReCAPTCHA ref={recaptchaRef} sitekey={SITE_KEY} />
        </div>

        <button type="submit" disabled={submitting}>
          {submitting ? 'Sending…' : 'Send Message'}
        </button>
      </form>
    </div>
  );
}
