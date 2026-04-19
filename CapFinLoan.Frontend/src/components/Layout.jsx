import { Sidebar } from './Sidebar';
import { Navbar } from './Navbar';
import { useNavigate } from 'react-router-dom';

export const Layout = ({ children }) => {
  const navigate = useNavigate();
  const user = JSON.parse(localStorage.getItem('user') || '{}');

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    navigate('/login');
  };

  return (
    <div className="flex min-h-screen bg-slate-50 dark:bg-slate-900">
      <Sidebar role={user.role} />
      <div className="flex-1 flex flex-col">
        <Navbar userName={user.name} onLogout={handleLogout} />
        <main className="flex-1 p-6">
          {children}
        </main>
      </div>
    </div>
  );
};
