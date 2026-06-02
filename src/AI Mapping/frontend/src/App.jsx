import { useMemo, useRef, useState } from 'react';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '';

const visibleColumns = [
  ['sourceRowNumber', 'Row'],
  ['firstName', 'First name'],
  ['lastName', 'Last name'],
  ['email', 'Email'],
  ['phoneNumber', 'Phone number'],
  ['company', 'Company'],
  ['jobTitle', 'Job title'],
  ['country', 'Country'],
  ['city', 'City'],
  ['signupDate', 'Signup date'],
  ['annualRevenue', 'Annual revenue']
];

export default function App() {
  const fileInputRef = useRef(null);
  const [selectedFile, setSelectedFile] = useState(null);
  const [analysis, setAnalysis] = useState(null);
  const [mapping, setMapping] = useState({});
  const [result, setResult] = useState(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const canComplete = analysis && !busy;

  async function handleFileChange(event) {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    setSelectedFile(file);
    setAnalysis(null);
    setResult(null);
    setError('');
    await analyzeFile(file);
  }

  async function analyzeFile(file) {
    setBusy(true);

    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await fetch(`${API_BASE_URL}/api/import/analyze`, {
        method: 'POST',
        body: formData
      });

      const payload = await readJson(response);
      if (!response.ok) {
        throw new Error(payload.message ?? 'Upload failed.');
      }

      const suggestedMapping = {};
      for (const sourceHeader of payload.sourceHeaders) {
        const suggestion = payload.suggestedMappings.find(
          (item) => item.sourceHeader.toLowerCase() === sourceHeader.toLowerCase()
        );
        suggestedMapping[sourceHeader] = suggestion?.targetKey ?? '';
      }

      setAnalysis(payload);
      setMapping(suggestedMapping);
      setModalOpen(true);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function completeImport() {
    if (!analysis) {
      return;
    }

    setBusy(true);
    setError('');

    try {
      const response = await fetch(`${API_BASE_URL}/api/import/complete`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          importId: analysis.importId,
          mappings: analysis.sourceHeaders.map((sourceHeader) => ({
            sourceHeader,
            targetKey: mapping[sourceHeader] || null
          }))
        })
      });

      const payload = await readJson(response);
      if (!response.ok) {
        throw new Error(payload.message ?? 'Mapping failed.');
      }

      setResult(payload);
      setModalOpen(false);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  function updateMapping(targetKey, sourceHeader) {
    setMapping((current) => {
      const next = { ...current };

      for (const currentSourceHeader of Object.keys(next)) {
        if (next[currentSourceHeader] === targetKey || currentSourceHeader === sourceHeader) {
          next[currentSourceHeader] = '';
        }
      }

      if (sourceHeader) {
        next[sourceHeader] = targetKey;
      }

      return next;
    });
  }

  return (
    <main className="app-shell">
      <section className="topbar">
        <div>
          <h1>AI Mapping Import</h1>
          <p>{selectedFile?.name ?? 'CSV or XLSX'}</p>
        </div>
        <div className="actions">
          <button type="button" onClick={() => fileInputRef.current?.click()} disabled={busy}>
            Upload file
          </button>
          {analysis && (
            <button type="button" className="secondary" onClick={() => setModalOpen(true)} disabled={busy}>
              Mapping
            </button>
          )}
        </div>
      </section>

      <input
        ref={fileInputRef}
        className="file-input"
        type="file"
        accept=".csv,.xlsx"
        onChange={handleFileChange}
      />

      {error && <div className="notice error">{error}</div>}

      {!analysis && (
        <section
          className="upload-zone"
          onClick={() => fileInputRef.current?.click()}
          onKeyDown={(event) => {
            if (event.key === 'Enter' || event.key === ' ') {
              fileInputRef.current?.click();
            }
          }}
          role="button"
          tabIndex={0}
        >
          <strong>{busy ? 'Reading file' : 'Select CSV or XLSX'}</strong>
          <span>Headers will be detected from the first rows.</span>
        </section>
      )}

      {analysis && (
        <ImportSummary analysis={analysis} mapping={mapping} result={result} />
      )}

      {result && <ResultGrid result={result} />}

      {analysis && modalOpen && (
        <MappingModal
          analysis={analysis}
          mapping={mapping}
          busy={busy}
          canComplete={canComplete}
          onChange={updateMapping}
          onCancel={() => setModalOpen(false)}
          onComplete={completeImport}
        />
      )}
    </main>
  );
}

function ImportSummary({ analysis, mapping, result }) {
  const mappedCount = Object.values(mapping).filter(Boolean).length;
  const aiCount = analysis.suggestedMappings.filter((item) => item.suggestedBy === 'OpenAI').length;

  return (
    <section className="summary-grid">
      <div className="metric">
        <span>Header row</span>
        <strong>{analysis.headerRowIndex + 1}</strong>
      </div>
      <div className="metric">
        <span>Columns</span>
        <strong>{analysis.sourceHeaders.length}</strong>
      </div>
      <div className="metric">
        <span>Mapped</span>
        <strong>{mappedCount}</strong>
      </div>
      <div className="metric">
        <span>AI suggestions</span>
        <strong>{aiCount || 'Fallback'}</strong>
      </div>
      {result && (
        <div className="metric">
          <span>Rows</span>
          <strong>{result.mappedRows}</strong>
        </div>
      )}
    </section>
  );
}

function MappingModal({ analysis, mapping, busy, canComplete, onChange, onCancel, onComplete }) {
  const selectedSourceByTarget = useMemo(() => {
    const selected = {};

    for (const [sourceHeader, targetKey] of Object.entries(mapping)) {
      if (targetKey) {
        selected[targetKey] = sourceHeader;
      }
    }

    return selected;
  }, [mapping]);

  const suggestionByTarget = useMemo(() => {
    const suggestions = {};

    for (const suggestion of analysis.suggestedMappings) {
      if (!suggestion.targetKey) {
        continue;
      }

      const current = suggestions[suggestion.targetKey];
      if (!current || suggestion.confidence > current.confidence) {
        suggestions[suggestion.targetKey] = suggestion;
      }
    }

    return suggestions;
  }, [analysis.suggestedMappings]);

  return (
    <div className="modal-backdrop" role="presentation">
      <section className="modal-panel" role="dialog" aria-modal="true" aria-labelledby="mapping-title">
        <header className="modal-header">
          <div>
            <h2 id="mapping-title">Confirm Mapping</h2>
            <p>{analysis.fileName}</p>
          </div>
          <button type="button" className="icon-button" aria-label="Close" onClick={onCancel}>
            x
          </button>
        </header>

        <div className="mapping-table-wrap">
          <table className="mapping-table">
            <thead>
              <tr>
                <th>Standard field</th>
                <th>Header in file</th>
                <th>Sample values</th>
                <th>Confidence</th>
              </tr>
            </thead>
            <tbody>
              {analysis.targetHeaders.map((targetHeader) => {
                const selectedSourceHeader = selectedSourceByTarget[targetHeader.key] ?? '';
                const suggestion = suggestionByTarget[targetHeader.key];
                const selectedSourceIndex = analysis.sourceHeaders.findIndex(
                  (sourceHeader) => sourceHeader.toLowerCase() === selectedSourceHeader.toLowerCase()
                );
                const sampleValues = selectedSourceIndex >= 0
                  ? analysis.previewRows
                    .map((row) => row[selectedSourceIndex])
                    .filter(Boolean)
                    .slice(0, 3)
                  : [];
                const confidence = selectedSourceHeader === suggestion?.sourceHeader
                  ? suggestion.confidence
                  : 0;

                return (
                  <tr key={targetHeader.key} className={targetHeader.required && !selectedSourceHeader ? 'missing-required' : ''}>
                    <td>
                      <strong>{targetHeader.displayName}</strong>
                      <span>
                        {targetHeader.key} - {targetHeader.dataType}
                        {targetHeader.required ? ' - Required' : ''}
                      </span>
                      <span>Aliases: {targetHeader.aliases.join(', ')}</span>
                    </td>
                    <td>
                      <select value={selectedSourceHeader} onChange={(event) => onChange(targetHeader.key, event.target.value)}>
                        <option value="">No source header</option>
                        {analysis.sourceHeaders.map((sourceHeader) => (
                          <option key={sourceHeader} value={sourceHeader}>
                            {sourceHeader}
                          </option>
                        ))}
                      </select>
                      <span>
                        {suggestion
                          ? `Suggested: ${suggestion.sourceHeader}`
                          : 'No suggestion'}
                      </span>
                    </td>
                    <td>{sampleValues.join(', ') || '-'}</td>
                    <td>
                      <span className="confidence">{Math.round(confidence * 100)}%</span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        <footer className="modal-actions">
          <button type="button" className="secondary" onClick={onCancel} disabled={busy}>
            Cancel
          </button>
          <button type="button" onClick={onComplete} disabled={!canComplete}>
            {busy ? 'Mapping' : 'Confirm'}
          </button>
        </footer>
      </section>
    </div>
  );
}

function ResultGrid({ result }) {
  return (
    <section className="grid-section">
      <header className="grid-header">
        <h2>Mapped Records</h2>
        <div className="grid-stats">
          <span>{result.mappedRows} mapped</span>
          <span>{result.skippedRows} skipped</span>
        </div>
      </header>

      {result.warnings.length > 0 && (
        <div className="notice warning">
          {result.warnings.map((warning) => (
            <span key={warning}>{warning}</span>
          ))}
        </div>
      )}

      <div className="data-grid-wrap">
        <table className="data-grid">
          <thead>
            <tr>
              {visibleColumns.map(([, label]) => (
                <th key={label}>{label}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {result.items.map((item) => (
              <tr key={item.sourceRowNumber}>
                {visibleColumns.map(([key]) => (
                  <td key={key}>{formatValue(item[key])}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

async function readJson(response) {
  const text = await response.text();
  return text ? JSON.parse(text) : {};
}

function formatValue(value) {
  if (value === null || value === undefined || value === '') {
    return '-';
  }

  return value;
}
