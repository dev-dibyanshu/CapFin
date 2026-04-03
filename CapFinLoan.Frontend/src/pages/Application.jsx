import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api/axios';

export default function Application() {
  const [applications, setApplications] = useState([]);
  const [selectedApp, setSelectedApp] = useState(null);
  const [formData, setFormData] = useState({
    personalDetails: {
      firstName: '', lastName: '', dateOfBirth: '', gender: '', email: '', phone: '',
      addressLine1: '', addressLine2: '', city: '', state: '', postalCode: ''
    },
    employmentDetails: {
      employerName: '', employmentType: '', monthlyIncome: 0, annualIncome: 0, existingEmiAmount: 0
    },
    loanDetails: {
      requestedAmount: 0, requestedTenureMonths: 0, loanPurpose: '', remarks: ''
    }
  });
  const navigate = useNavigate();

  useEffect(() => {
    fetchApplications();
  }, []);

  const fetchApplications = async () => {
    try {
      console.log('[Fetching Applications]');
      const response = await api.get('/applications/my');
      setApplications(response.data);
    } catch (err) {
      console.error('[Fetch Applications Error]', err);
      if (err.response?.status === 401) {
        alert('Session expired. Please login again.');
        localStorage.clear();
        navigate('/');
      }
    }
  };

  const handleCreate = async () => {
    try {
      console.log('[Creating Application] - No request body');
      // Create draft application without body - backend will create with empty values
      const response = await api.post('/applications', {
        personalDetails: {
          firstName: '',
          lastName: '',
          dateOfBirth: null,
          gender: '',
          email: '',
          phone: '',
          addressLine1: '',
          addressLine2: '',
          city: '',
          state: '',
          postalCode: ''
        },
        employmentDetails: {
          employerName: '',
          employmentType: '',
          monthlyIncome: null,
          annualIncome: null,
          existingEmiAmount: 0
        },
        loanDetails: {
          requestedAmount: 0,
          requestedTenureMonths: 0,
          loanPurpose: '',
          remarks: ''
        }
      });
      console.log('[Application Created]', response.data);
      alert('Application created! You can now update the details.');
      setSelectedApp(response.data);
      fetchApplications();
    } catch (err) {
      console.error('[Create Application Error]', err);
      const errorMessage = err.response?.data?.message || err.message || 'Failed to create application';
      alert(errorMessage);
    }
  };

  const handleUpdate = async (section) => {
    if (!selectedApp) return;
    
    try {
      console.log(`[Updating ${section}]`, formData[section.replace('-', '')]);
      const sectionData = section === 'personal-details' ? formData.personalDetails :
                          section === 'employment-details' ? formData.employmentDetails :
                          formData.loanDetails;
      
      await api.put(`/applications/${selectedApp.id}/${section}`, sectionData);
      alert(`${section} updated!`);
      fetchApplications();
    } catch (err) {
      console.error(`[Update ${section} Error]`, err);
      const errorMessage = err.response?.data?.message || err.message || 'Failed to update';
      alert(errorMessage);
    }
  };

  const handleSubmit = async () => {
    if (!selectedApp) return;
    
    try {
      console.log('[Submitting Application]', selectedApp.id);
      await api.post(`/applications/${selectedApp.id}/submit`);
      alert('Application submitted successfully!');
      setSelectedApp(null);
      fetchApplications();
    } catch (err) {
      console.error('[Submit Application Error]', err);
      const errorMessage = err.response?.data?.message || err.message || 'Failed to submit application';
      alert(errorMessage);
    }
  };

  const handleLogout = () => {
    localStorage.clear();
    navigate('/');
  };

  return (
    <div style={{ padding: '20px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h2>My Applications</h2>
        <button onClick={handleLogout} style={{ padding: '10px 20px' }}>Logout</button>
      </div>

      <div style={{ marginBottom: '20px' }}>
        <button onClick={handleCreate} style={{ padding: '10px 20px', marginRight: '10px', backgroundColor: '#28a745', color: 'white', border: 'none', cursor: 'pointer' }}>
          Create New Application
        </button>
      </div>

      <h3>Existing Applications</h3>
      {applications.length === 0 ? (
        <p>No applications found. Create your first application!</p>
      ) : (
        <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: '20px' }}>
          <thead>
            <tr style={{ backgroundColor: '#f8f9fa' }}>
              <th style={{ border: '1px solid #dee2e6', padding: '10px' }}>Application Number</th>
              <th style={{ border: '1px solid #dee2e6', padding: '10px' }}>Status</th>
              <th style={{ border: '1px solid #dee2e6', padding: '10px' }}>Created</th>
              <th style={{ border: '1px solid #dee2e6', padding: '10px' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {applications.map((app) => (
              <tr key={app.id}>
                <td style={{ border: '1px solid #dee2e6', padding: '10px' }}>{app.applicationNumber}</td>
                <td style={{ border: '1px solid #dee2e6', padding: '10px' }}>{app.status}</td>
                <td style={{ border: '1px solid #dee2e6', padding: '10px' }}>{new Date(app.createdAtUtc).toLocaleDateString()}</td>
                <td style={{ border: '1px solid #dee2e6', padding: '10px' }}>
                  <button onClick={() => setSelectedApp(app)} style={{ marginRight: '5px', padding: '5px 10px' }}>Edit</button>
                  <button onClick={() => navigate('/upload', { state: { applicationId: app.id, applicationNumber: app.applicationNumber } })} style={{ marginRight: '5px', padding: '5px 10px', backgroundColor: '#17a2b8', color: 'white', border: 'none', cursor: 'pointer' }}>
                    Upload Docs
                  </button>
                  {app.status === 'Draft' && (
                    <button onClick={() => { setSelectedApp(app); setTimeout(handleSubmit, 100); }} style={{ padding: '5px 10px', backgroundColor: '#007bff', color: 'white', border: 'none', cursor: 'pointer' }}>
                      Submit
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {selectedApp && selectedApp.status === 'Draft' && (
        <div style={{ border: '1px solid #ccc', padding: '20px', marginTop: '20px' }}>
          <h3>Edit Application: {selectedApp.applicationNumber}</h3>
          
          <h4>Personal Details</h4>
          <input placeholder="First Name" value={formData.personalDetails.firstName} onChange={(e) => setFormData({...formData, personalDetails: {...formData.personalDetails, firstName: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input placeholder="Last Name" value={formData.personalDetails.lastName} onChange={(e) => setFormData({...formData, personalDetails: {...formData.personalDetails, lastName: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input type="date" placeholder="Date of Birth" value={formData.personalDetails.dateOfBirth} onChange={(e) => setFormData({...formData, personalDetails: {...formData.personalDetails, dateOfBirth: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input placeholder="Gender" value={formData.personalDetails.gender} onChange={(e) => setFormData({...formData, personalDetails: {...formData.personalDetails, gender: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input placeholder="Email" value={formData.personalDetails.email} onChange={(e) => setFormData({...formData, personalDetails: {...formData.personalDetails, email: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input placeholder="Phone" value={formData.personalDetails.phone} onChange={(e) => setFormData({...formData, personalDetails: {...formData.personalDetails, phone: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input placeholder="Address Line 1" value={formData.personalDetails.addressLine1} onChange={(e) => setFormData({...formData, personalDetails: {...formData.personalDetails, addressLine1: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input placeholder="Address Line 2" value={formData.personalDetails.addressLine2} onChange={(e) => setFormData({...formData, personalDetails: {...formData.personalDetails, addressLine2: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input placeholder="City" value={formData.personalDetails.city} onChange={(e) => setFormData({...formData, personalDetails: {...formData.personalDetails, city: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input placeholder="State" value={formData.personalDetails.state} onChange={(e) => setFormData({...formData, personalDetails: {...formData.personalDetails, state: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input placeholder="Postal Code" value={formData.personalDetails.postalCode} onChange={(e) => setFormData({...formData, personalDetails: {...formData.personalDetails, postalCode: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <button onClick={() => handleUpdate('personal-details')} style={{ padding: '10px 20px', backgroundColor: '#007bff', color: 'white', border: 'none', cursor: 'pointer', marginBottom: '20px' }}>Update Personal Details</button>

          <h4>Employment Details</h4>
          <input placeholder="Employer Name" value={formData.employmentDetails.employerName} onChange={(e) => setFormData({...formData, employmentDetails: {...formData.employmentDetails, employerName: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input placeholder="Employment Type" value={formData.employmentDetails.employmentType} onChange={(e) => setFormData({...formData, employmentDetails: {...formData.employmentDetails, employmentType: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input type="number" placeholder="Monthly Income" value={formData.employmentDetails.monthlyIncome} onChange={(e) => setFormData({...formData, employmentDetails: {...formData.employmentDetails, monthlyIncome: parseFloat(e.target.value) || 0}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input type="number" placeholder="Annual Income" value={formData.employmentDetails.annualIncome} onChange={(e) => setFormData({...formData, employmentDetails: {...formData.employmentDetails, annualIncome: parseFloat(e.target.value) || 0}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input type="number" placeholder="Existing EMI Amount" value={formData.employmentDetails.existingEmiAmount} onChange={(e) => setFormData({...formData, employmentDetails: {...formData.employmentDetails, existingEmiAmount: parseFloat(e.target.value) || 0}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <button onClick={() => handleUpdate('employment-details')} style={{ padding: '10px 20px', backgroundColor: '#007bff', color: 'white', border: 'none', cursor: 'pointer', marginBottom: '20px' }}>Update Employment Details</button>

          <h4>Loan Details</h4>
          <input type="number" placeholder="Requested Amount" value={formData.loanDetails.requestedAmount} onChange={(e) => setFormData({...formData, loanDetails: {...formData.loanDetails, requestedAmount: parseFloat(e.target.value) || 0}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input type="number" placeholder="Requested Tenure (Months)" value={formData.loanDetails.requestedTenureMonths} onChange={(e) => setFormData({...formData, loanDetails: {...formData.loanDetails, requestedTenureMonths: parseInt(e.target.value) || 0}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <input placeholder="Loan Purpose" value={formData.loanDetails.loanPurpose} onChange={(e) => setFormData({...formData, loanDetails: {...formData.loanDetails, loanPurpose: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px' }} />
          <textarea placeholder="Remarks" value={formData.loanDetails.remarks} onChange={(e) => setFormData({...formData, loanDetails: {...formData.loanDetails, remarks: e.target.value}})} style={{ width: '100%', padding: '8px', marginBottom: '10px', minHeight: '80px' }} />
          <button onClick={() => handleUpdate('loan-details')} style={{ padding: '10px 20px', backgroundColor: '#007bff', color: 'white', border: 'none', cursor: 'pointer' }}>Update Loan Details</button>
        </div>
      )}
    </div>
  );
}
