import { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import api from '../api/axios';

export default function DocumentUpload() {
  const location = useLocation();
  const navigate = useNavigate();
  const [file, setFile] = useState(null);
  const [documentType, setDocumentType] = useState('Identity Proof');
  const [uploading, setUploading] = useState(false);
  
  // Get application details from navigation state
  const applicationId = location.state?.applicationId;
  const applicationNumber = location.state?.applicationNumber;

  useEffect(() => {
    if (!applicationId) {
      alert('No application selected. Please select an application first.');
      navigate('/app');
    }
  }, [applicationId, navigate]);

  const handleUpload = async (e) => {
    e.preventDefault();
    if (!file) {
      alert('Please select a file');
      return;
    }

    if (!applicationId) {
      alert('Application ID is missing');
      return;
    }

    setUploading(true);
    const formData = new FormData();
    formData.append('File', file);
    formData.append('LoanApplicationId', applicationId);
    formData.append('DocumentType', documentType);

    console.log('Uploading document:', {
      fileName: file.name,
      fileSize: file.size,
      fileType: file.type,
      loanApplicationId: applicationId,
      applicationNumber: applicationNumber,
      documentType
    });

    try {
      const response = await api.post('/documents/upload', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      console.log('Upload success:', response.data);
      alert('Document uploaded successfully!');
      setFile(null);
      document.querySelector('input[type="file"]').value = '';
    } catch (err) {
      console.error('Upload error:', err.response?.data || err.message);
      alert(err.response?.data?.message || 'Failed to upload document');
    } finally {
      setUploading(false);
    }
  };

  return (
    <div style={{ maxWidth: '600px', margin: '50px auto', padding: '20px', border: '1px solid #ccc' }}>
      <h2>Upload Document</h2>
      {applicationNumber && (
        <div style={{ marginBottom: '20px', padding: '10px', backgroundColor: '#e7f3ff', border: '1px solid #b3d9ff', borderRadius: '4px' }}>
          <strong>Application:</strong> {applicationNumber}
        </div>
      )}
      <form onSubmit={handleUpload}>
        <div style={{ marginBottom: '10px' }}>
          <label>Document Type:</label>
          <select
            value={documentType}
            onChange={(e) => setDocumentType(e.target.value)}
            style={{ width: '100%', padding: '8px', marginTop: '5px' }}
          >
            <option>Identity Proof</option>
            <option>Address Proof</option>
            <option>Income Proof</option>
            <option>Bank Statement</option>
            <option>Employment Proof</option>
            <option>Other</option>
          </select>
        </div>
        <div style={{ marginBottom: '10px' }}>
          <label>File:</label>
          <input
            type="file"
            onChange={(e) => setFile(e.target.files[0])}
            required
            accept=".pdf,.jpg,.jpeg,.png"
            style={{ width: '100%', padding: '8px', marginTop: '5px' }}
          />
          <small style={{ color: '#666' }}>Allowed: PDF, JPG, PNG (Max 5MB)</small>
        </div>
        <button
          type="submit"
          disabled={uploading}
          style={{ width: '100%', padding: '10px', backgroundColor: '#007bff', color: 'white', border: 'none', cursor: 'pointer' }}
        >
          {uploading ? 'Uploading...' : 'Upload Document'}
        </button>
      </form>
      <button
        onClick={() => navigate('/app')}
        style={{ width: '100%', padding: '10px', marginTop: '10px', backgroundColor: '#6c757d', color: 'white', border: 'none', cursor: 'pointer' }}
      >
        Back to Applications
      </button>
    </div>
  );
}
