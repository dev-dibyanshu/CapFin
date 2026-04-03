import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api/axios';

export default function Admin() {
  const [applications, setApplications] = useState([]);
  const [selectedApp, setSelectedApp] = useState(null);
  const [documents, setDocuments] = useState([]);
  const [targetStatus, setTargetStatus] = useState('Under Review');
  const [remarks, setRemarks] = useState('');
  const navigate = useNavigate();

  useEffect(() => {
    fetchApplications();
  }, []);

  useEffect(() => {
    if (selectedApp?.id) {
      fetchDocuments(selectedApp.id);
    } else {
      setDocuments([]);
    }
  }, [selectedApp]);

  const fetchApplications = async () => {
    try {
      const response = await api.get('/admin/applications');
      setApplications(response.data);
    } catch (err) {
      console.error(err);
    }
  };

  const fetchDocuments = async (applicationId) => {
    try {
      console.log('Fetching documents for application:', applicationId);
      const response = await api.get(`/documents/application/${applicationId}`);
      console.log('Documents response:', response.data);
      setDocuments(response.data.data || response.data || []);
    } catch (err) {
      console.error('Document fetch error:', err);
      setDocuments([]);
    }
  };

  const handleDownload = async (docId, fileName) => {
    try {
      console.log('Downloading document:', docId);
      const response = await api.get(`/documents/${docId}/download`, {
        responseType: 'blob', // Important for file download
      });
      
      // Create blob URL and trigger download
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', fileName || 'document');
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
      
      console.log('Download successful');
    } catch (err) {
      console.error('Download error:', err);
      alert(err.response?.data?.message || 'Failed to download document');
    }
  };

  const handleUpdateStatus = async () => {
    if (!selectedApp) return;

    try {
      await api.put(`/admin/applications/${selectedApp.id}/status`, {
        targetStatus,
        remarks
      });
      alert('Status updated successfully!');
      setSelectedApp(null);
      setRemarks('');
      fetchApplications();
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to update status');
    }
  };

  const handleLogout = () => {
    localStorage.clear();
    navigate('/');
  };

  return (
    <div style={{ padding: '20px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h2>Admin Panel - Applications</h2>
        <button onClick={handleLogout} style={{ padding: '10px 20px' }}>Logout</button>
      </div>

      <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: '20px' }}>
        <thead>
          <tr style={{ backgroundColor: '#f8f9fa' }}>
            <th style={{ border: '1px solid #dee2e6', padding: '10px' }}>Application Number</th>
            <th style={{ border: '1px solid #dee2e6', padding: '10px' }}>Applicant</th>
            <th style={{ border: '1px solid #dee2e6', padding: '10px' }}>Amount</th>
            <th style={{ border: '1px solid #dee2e6', padding: '10px' }}>Status</th>
            <th style={{ border: '1px solid #dee2e6', padding: '10px' }}>Submitted</th>
            <th style={{ border: '1px solid #dee2e6', padding: '10px' }}>Actions</th>
          </tr>
        </thead>
        <tbody>
          {applications.map((app) => (
            <tr key={app.id}>
              <td style={{ border: '1px solid #dee2e6', padding: '10px' }}>{app.applicationNumber}</td>
              <td style={{ border: '1px solid #dee2e6', padding: '10px' }}>{app.applicantName}</td>
              <td style={{ border: '1px solid #dee2e6', padding: '10px' }}>${app.requestedAmount}</td>
              <td style={{ border: '1px solid #dee2e6', padding: '10px' }}>{app.status}</td>
              <td style={{ border: '1px solid #dee2e6', padding: '10px' }}>
                {app.submittedAtUtc ? new Date(app.submittedAtUtc).toLocaleDateString() : 'N/A'}
              </td>
              <td style={{ border: '1px solid #dee2e6', padding: '10px' }}>
                <button
                  onClick={() => setSelectedApp(app)}
                  style={{ padding: '5px 10px', backgroundColor: '#007bff', color: 'white', border: 'none', cursor: 'pointer' }}
                >
                  Review
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {selectedApp && (
        <div style={{ border: '1px solid #ccc', padding: '20px', marginTop: '20px', backgroundColor: '#f8f9fa' }}>
          <h3>Review Application: {selectedApp.applicationNumber}</h3>
          <p><strong>Current Status:</strong> {selectedApp.status}</p>
          <p><strong>Applicant:</strong> {selectedApp.applicantName}</p>
          <p><strong>Email:</strong> {selectedApp.email}</p>
          <p><strong>Phone:</strong> {selectedApp.phone}</p>
          <p><strong>Requested Amount:</strong> ${selectedApp.requestedAmount}</p>
          <p><strong>Tenure:</strong> {selectedApp.requestedTenureMonths} months</p>

          <div style={{ marginTop: '20px', marginBottom: '20px', padding: '15px', backgroundColor: 'white', border: '1px solid #dee2e6' }}>
            <h4>Uploaded Documents</h4>
            {documents.length === 0 ? (
              <p style={{ color: '#666', fontStyle: 'italic' }}>No documents uploaded yet</p>
            ) : (
              <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '10px' }}>
                <thead>
                  <tr style={{ backgroundColor: '#f8f9fa' }}>
                    <th style={{ border: '1px solid #dee2e6', padding: '8px', textAlign: 'left' }}>Document Type</th>
                    <th style={{ border: '1px solid #dee2e6', padding: '8px', textAlign: 'left' }}>File Name</th>
                    <th style={{ border: '1px solid #dee2e6', padding: '8px', textAlign: 'left' }}>Uploaded</th>
                    <th style={{ border: '1px solid #dee2e6', padding: '8px', textAlign: 'left' }}>Status</th>
                    <th style={{ border: '1px solid #dee2e6', padding: '8px', textAlign: 'center' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {documents.map((doc) => (
                    <tr key={doc.id}>
                      <td style={{ border: '1px solid #dee2e6', padding: '8px' }}>{doc.documentType}</td>
                      <td style={{ border: '1px solid #dee2e6', padding: '8px' }}>{doc.fileName}</td>
                      <td style={{ border: '1px solid #dee2e6', padding: '8px' }}>
                        {new Date(doc.uploadedAtUtc).toLocaleDateString()}
                      </td>
                      <td style={{ border: '1px solid #dee2e6', padding: '8px' }}>
                        <span style={{ 
                          padding: '3px 8px', 
                          borderRadius: '3px',
                          backgroundColor: doc.verificationStatus === 'Verified' ? '#d4edda' : 
                                         doc.verificationStatus === 'Rejected' ? '#f8d7da' : '#fff3cd',
                          color: doc.verificationStatus === 'Verified' ? '#155724' : 
                                 doc.verificationStatus === 'Rejected' ? '#721c24' : '#856404'
                        }}>
                          {doc.verificationStatus}
                        </span>
                      </td>
                      <td style={{ border: '1px solid #dee2e6', padding: '8px', textAlign: 'center' }}>
                        <button
                          onClick={() => handleDownload(doc.id, doc.fileName)}
                          style={{ 
                            padding: '5px 10px', 
                            backgroundColor: '#17a2b8', 
                            color: 'white', 
                            border: 'none',
                            borderRadius: '3px',
                            cursor: 'pointer'
                          }}
                        >
                          Download
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          <div style={{ marginTop: '20px' }}>
            <label>Update Status:</label>
            <select
              value={targetStatus}
              onChange={(e) => setTargetStatus(e.target.value)}
              style={{ width: '100%', padding: '8px', marginTop: '5px', marginBottom: '10px' }}
            >
              <option>Docs Pending</option>
              <option>Under Review</option>
              <option>Approved</option>
              <option>Rejected</option>
            </select>

            <label>Remarks:</label>
            <textarea
              value={remarks}
              onChange={(e) => setRemarks(e.target.value)}
              placeholder="Enter remarks (required for rejection)"
              style={{ width: '100%', padding: '8px', marginTop: '5px', marginBottom: '10px', minHeight: '80px' }}
            />

            <div>
              <button
                onClick={handleUpdateStatus}
                style={{ padding: '10px 20px', marginRight: '10px', backgroundColor: '#28a745', color: 'white', border: 'none', cursor: 'pointer' }}
              >
                Update Status
              </button>
              <button
                onClick={() => setSelectedApp(null)}
                style={{ padding: '10px 20px', backgroundColor: '#6c757d', color: 'white', border: 'none', cursor: 'pointer' }}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
