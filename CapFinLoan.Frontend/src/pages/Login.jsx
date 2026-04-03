import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api/axios';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isSignup, setIsSignup] = useState(false);
  const [name, setName] = useState('');
  const [phone, setPhone] = useState('');
  const [role, setRole] = useState('APPLICANT');
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    try {
      if (isSignup) {
        // ✅ Basic validation
        if (!name.trim()) {
          setError('Name is required');
          return;
        }
        if (!phone.trim()) {
          setError('Phone number is required');
          return;
        }

        const res = await api.post('/auth/signup', {
          name,
          email,
          password,
          role,
          phone,
        });

        console.log('Signup response:', res.data);

        // ✅ SUCCESS HANDLING (FIXED)
        alert('Signup successful ✅ Please login now.');

        // Reset form
        setIsSignup(false);
        setName('');
        setPhone('');
        setEmail('');
        setPassword('');
      } else {
        const response = await api.post('/auth/login', { email, password });

        console.log('Login response:', response.data);

        const { token, role: userRole } = response.data;

        // ✅ Store token
        localStorage.setItem('token', token);
        localStorage.setItem('role', userRole);

        // ✅ Redirect based on role
        if (userRole === 'ADMIN') {
          navigate('/admin');
        } else {
          navigate('/app');
        }
      }
    } catch (err) {
      console.log('ERROR FULL:', err);
      console.log('ERROR RESPONSE:', err.response);

      // ✅ FIXED ERROR HANDLING
      let message = 'Something went wrong';

      if (err.response?.data?.message) {
        message = err.response.data.message;
      } else if (err.response?.data?.errors) {
        // ASP.NET validation errors
        message = Object.values(err.response.data.errors).flat().join(', ');
      } else if (err.message) {
        message = err.message;
      }

      setError(message);
    }
  };

  return (
    <div style={{ maxWidth: '400px', margin: '50px auto', padding: '20px', border: '1px solid #ccc' }}>
      <h2>{isSignup ? 'Sign Up' : 'Login'}</h2>

      <form onSubmit={handleSubmit}>
        {isSignup && (
          <>
            <div style={{ marginBottom: '10px' }}>
              <label>Name:</label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                style={{ width: '100%', padding: '8px', marginTop: '5px' }}
              />
            </div>

            <div style={{ marginBottom: '10px' }}>
              <label>Phone:</label>
              <input
                type="tel"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                required
                placeholder="Enter phone number"
                style={{ width: '100%', padding: '8px', marginTop: '5px' }}
              />
            </div>

            <div style={{ marginBottom: '10px' }}>
              <label>Role:</label>
              <select
                value={role}
                onChange={(e) => setRole(e.target.value)}
                style={{ width: '100%', padding: '8px', marginTop: '5px' }}
              >
                <option value="APPLICANT">Applicant</option>
                <option value="ADMIN">Admin</option>
              </select>
            </div>
          </>
        )}

        <div style={{ marginBottom: '10px' }}>
          <label>Email:</label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            style={{ width: '100%', padding: '8px', marginTop: '5px' }}
          />
        </div>

        <div style={{ marginBottom: '10px' }}>
          <label>Password:</label>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            style={{ width: '100%', padding: '8px', marginTop: '5px' }}
          />
        </div>

        {error && (
          <div style={{ color: 'red', marginBottom: '10px' }}>
            {error}
          </div>
        )}

        <button
          type="submit"
          style={{
            width: '100%',
            padding: '10px',
            backgroundColor: '#007bff',
            color: 'white',
            border: 'none',
            cursor: 'pointer'
          }}
        >
          {isSignup ? 'Sign Up' : 'Login'}
        </button>
      </form>

      <button
        onClick={() => {
          setError('');
          setIsSignup(!isSignup);
        }}
        style={{
          width: '100%',
          padding: '10px',
          marginTop: '10px',
          backgroundColor: '#6c757d',
          color: 'white',
          border: 'none',
          cursor: 'pointer'
        }}
      >
        {isSignup ? 'Already have an account? Login' : 'Need an account? Sign Up'}
      </button>
    </div>
  );
}