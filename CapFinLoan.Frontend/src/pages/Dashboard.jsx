import { useState, useEffect } from 'react';
import { FileText, CheckCircle, Clock, XCircle } from 'lucide-react';
import { Layout } from '../components/Layout';
import { Card } from '../components/Card';
import api from '../api/axios';

export default function Dashboard() {
  const [stats, setStats] = useState({
    total: 0,
    draft: 0,
    submitted: 0,
    approved: 0,
    rejected: 0,
  });

  useEffect(() => {
    fetchApplications();
  }, []);

  const fetchApplications = async () => {
    try {
      const response = await api.get('/applications');
      const applications = response.data;
      
      setStats({
        total: applications.length,
        draft: applications.filter(app => app.status === 'Draft').length,
        submitted: applications.filter(app => app.status === 'Submitted').length,
        approved: applications.filter(app => app.status === 'Approved').length,
        rejected: applications.filter(app => app.status === 'Rejected').length,
      });
    } catch (error) {
      console.error('Error fetching applications:', error);
    }
  };

  const statCards = [
    {
      title: 'Total Applications',
      value: stats.total,
      icon: FileText,
      color: 'text-blue-600 dark:text-blue-400',
      bgColor: 'bg-blue-50 dark:bg-blue-900/20',
    },
    {
      title: 'Submitted',
      value: stats.submitted,
      icon: Clock,
      color: 'text-yellow-600 dark:text-yellow-400',
      bgColor: 'bg-yellow-50 dark:bg-yellow-900/20',
    },
    {
      title: 'Approved',
      value: stats.approved,
      icon: CheckCircle,
      color: 'text-green-600 dark:text-green-400',
      bgColor: 'bg-green-50 dark:bg-green-900/20',
    },
    {
      title: 'Rejected',
      value: stats.rejected,
      icon: XCircle,
      color: 'text-red-600 dark:text-red-400',
      bgColor: 'bg-red-50 dark:bg-red-900/20',
    },
  ];

  return (
    <Layout>
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-slate-900 dark:text-slate-100">Dashboard</h1>
          <p className="text-slate-600 dark:text-slate-400 mt-1">Overview of your loan applications</p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {statCards.map((stat) => {
            const Icon = stat.icon;
            return (
              <Card key={stat.title} className="hover:shadow-lg transition-shadow">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-slate-600 dark:text-slate-400">{stat.title}</p>
                    <p className="text-3xl font-bold text-slate-900 dark:text-slate-100 mt-2">{stat.value}</p>
                  </div>
                  <div className={`p-3 rounded-xl ${stat.bgColor}`}>
                    <Icon className={`h-8 w-8 ${stat.color}`} />
                  </div>
                </div>
              </Card>
            );
          })}
        </div>

        <Card title="Quick Actions" icon={FileText}>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <a
              href="/application"
              className="p-4 border-2 border-dashed border-slate-300 dark:border-slate-600 rounded-lg hover:border-primary-500 dark:hover:border-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/10 transition-all text-center group"
            >
              <FileText className="h-8 w-8 mx-auto text-slate-400 group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors" />
              <p className="mt-2 font-medium text-slate-700 dark:text-slate-300">New Application</p>
            </a>
            <a
              href="/documents"
              className="p-4 border-2 border-dashed border-slate-300 dark:border-slate-600 rounded-lg hover:border-primary-500 dark:hover:border-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/10 transition-all text-center group"
            >
              <Clock className="h-8 w-8 mx-auto text-slate-400 group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors" />
              <p className="mt-2 font-medium text-slate-700 dark:text-slate-300">Upload Documents</p>
            </a>
            <a
              href="/application"
              className="p-4 border-2 border-dashed border-slate-300 dark:border-slate-600 rounded-lg hover:border-primary-500 dark:hover:border-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/10 transition-all text-center group"
            >
              <CheckCircle className="h-8 w-8 mx-auto text-slate-400 group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors" />
              <p className="mt-2 font-medium text-slate-700 dark:text-slate-300">View Applications</p>
            </a>
          </div>
        </Card>
      </div>
    </Layout>
  );
}
