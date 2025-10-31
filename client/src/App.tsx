import { use, useState } from 'react';
import * as XLSX from 'xlsx';
import Papa from 'papaparse';
import { BASE_URL } from '../src/constant';

type Item = {
  sku: string;
};

type TabData = {
  successful: Item[];
  unsuccessful: Item[];
  successCount?: number;
  failCount?: number;
};

function App() {
  const [file, setFile] = useState<File | null>(null);
  const [activeTab, setActiveTab] = useState<'Infigo' | 'Siteflow'>('Infigo');

  const [successfulSiteflow, setSuccessfulSiteflow] = useState<Item[]>([]);
  const [unsuccessfulSiteflow, setUnsuccessfulSiteflow] = useState<Item[]>([]);
  const [successfulInfigo, setSuccessfulInfigo] = useState<Item[]>([]);
  const [unsuccessfulInfigo, setUnsuccessfulInfigo] = useState<Item[]>([]);
  const [countSiteflowSuccess, setCountSiteflowSuccess] = useState(0);
  const [countSiteflowFails, setCountSiteflowFails] = useState(0);
  const [countInfigoSuccess, setCountInfigoSuccess] = useState(0);
  const [countInfigoFails, setCountInfigoFails] = useState(0);
  const [target, setTarget] = useState<
    'Moderna' | 'Syndax' | 'Pharvaris' | 'TestSite'
  >('Moderna');

  const tabDataMap: Record<'Siteflow' | 'Infigo', TabData> = {
    Siteflow: {
      successful: successfulSiteflow,
      unsuccessful: unsuccessfulSiteflow,
      successCount: countSiteflowSuccess,
      failCount: countSiteflowFails,
    },
    Infigo: {
      successful: successfulInfigo,
      unsuccessful: unsuccessfulInfigo,
      successCount: countInfigoSuccess,
      failCount: countInfigoFails,
    },
  };

  const { successful, unsuccessful, successCount, failCount } =
    tabDataMap[activeTab];

  const handleUpload = (
    target: 'Siteflow' | 'Infigo' | 'Both',
    targetSite?: 'Moderna' | 'Syndax' | 'Pharvaris' | 'TestSite',
  ) => {
    if (!file) {
      alert('Please select a file!');
      return;
    }

    const fileName = file.name.toLowerCase();

    const processData = (jsonData: unknown[]) => {
      if ((target === 'Infigo' || target === 'Both') && targetSite) {
        // Clear previous results for this target
        setSuccessfulInfigo([]);
        setUnsuccessfulInfigo([]);
        setCountInfigoSuccess(0);
        setCountInfigoFails(0);

        infigoSync(jsonData, targetSite);
      }

      if (target === 'Siteflow' || (target === 'Both' && targetSite)) {
        setSuccessfulSiteflow([]);
        setUnsuccessfulSiteflow([]);
        setCountSiteflowSuccess(0);
        setCountSiteflowFails(0);

        siteflowSync(jsonData, targetSite);
      }
    };

    if (fileName.endsWith('.csv')) {
      // Parse CSV
      Papa.parse(file, {
        header: true, // treat first row as headers
        complete: (results) => {
          console.log('CSV JSON data:', results.data);
          processData(results.data);
        },
        error: (err) => {
          console.error('CSV parse error:', err);
        },
      });
    } else if (fileName.endsWith('.xlsx') || fileName.endsWith('.xls')) {
      // Parse Excel
      const reader = new FileReader();
      reader.onload = (e) => {
        const data = e.target?.result;
        if (!data) return;

        const workbook = XLSX.read(data, { type: 'array' });
        const sheetName = workbook.SheetNames[0];
        const worksheet = workbook.Sheets[sheetName];
        const jsonData = XLSX.utils.sheet_to_json(worksheet, { defval: null });
        console.log('Excel JSON data:', jsonData);
        processData(jsonData);
      };
      reader.readAsArrayBuffer(file);
    } else {
      alert('Unsupported file type. Please upload CSV or Excel.');
    }

    setFile(null);

    // Also reset the input field so user can select the same file again
    const input = document.getElementById('file-upload') as HTMLInputElement;
    if (input) input.value = '';
  };

  const siteflowSync = async (
    jsonData: unknown[],
    targetSite: 'Moderna' | 'Syndax' | 'Pharvaris' | 'TestSite',
  ) => {
    console.log('Inside Siteflow Sync calling endpoint');
    console.log(`Target site: ${targetSite}`);

    const payload = {
      data: jsonData,
      targetSite: targetSite,
    };

    try {
      const res = await fetch(`${BASE_URL}/api/siteflow/sync`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      const data = await res.json();
      console.log('Backend response:', data);

      console.log(data.successSkus);

      setSuccessfulSiteflow(
        (data.successSkus || []).map((sku: string) => ({ sku })),
      );
      setUnsuccessfulSiteflow(
        (data.failedSkus || []).map((sku: string) => ({ sku })),
      );
      setCountSiteflowFails(data.failed);
      setCountSiteflowSuccess(data.successful);
    } catch (err) {
      console.error('Error sending data to backend:', err);
    }
  };

  const infigoSync = async (
    jsonData: unknown[],
    targetSite: 'Moderna' | 'Syndax' | 'Pharvaris' | 'TestSite',
  ) => {
    console.log('Inside Infigo Sync calling endpoint');
    console.log(`Target site: ${targetSite}`);

    const payload = {
      data: jsonData,
      targetSite: targetSite,
    };
    try {
      const res = await fetch(`${BASE_URL}/api/infigo/sync`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      const data = await res.json();
      console.log('Backend response:', data);

      setSuccessfulInfigo(
        (data.successSkus || []).map((sku: string) => ({ sku })),
      );
      setUnsuccessfulInfigo(
        (data.failedSkus || []).map((sku: string) => ({ sku })),
      );
      setCountInfigoFails(data.failed);
      setCountInfigoSuccess(data.successful);
    } catch (err) {
      console.error('Error sending data to backend:', err);
    }
  };

  return (
    <div className='flex h-screen bg-gray-50'>
      {/* Left sidebar */}
      <div className='w-[35%] bg-white border-r border-gray-200 p-8 flex flex-col mr-'>
        <div className='flex-1 flex flex-col justify-center'>
          <h1 className='text-xl text-gray-900 mb-8 justify-center flex font-extrabold'>
            File Upload
          </h1>

          <div className='space-y-4'>
            <div className='relative'>
              <input
                type='file'
                id='file-upload'
                onChange={(e) => {
                  const selectedFile = e.target.files?.[0] ?? null;
                  setFile(selectedFile);
                }}
                className='hidden'
              />
              <label
                htmlFor='file-upload'
                className='flex items-center justify-center w-full h-30 border-2 border-gray-300 rounded-lg cursor-pointer hover:border-gray-400 transition-colors'
              >
                <div className='text-center'>
                  <p className='text-sm text-gray-600'>
                    {file ? file.name : 'Choose file or drag here'}
                  </p>
                  <p className='text-xs text-gray-400 mt-1'>CSV or Excel</p>
                </div>
              </label>
            </div>

            <div>
              <label className='block text-gray-700 text-sm mb-1'>
                Select Infigo Target
              </label>
              <select
                value={target}
                onChange={(e) =>
                  setTarget(
                    e.target.value as
                      | 'Moderna'
                      | 'Syndax'
                      | 'Pharvaris'
                      | 'TestSite',
                  )
                }
                className='w-full border border-gray-300 rounded-lg px-3 py-2'
              >
                <option value='Moderna'>Moderna</option>
                <option value='Syndax'>Syndax</option>
                <option value='Pharvaris'>Pharvaris</option>
                <option value='TestSite'>TestSite</option>
              </select>
            </div>

            <div className='flex gap-4'>
              <button
                onClick={() => handleUpload('Siteflow', target)}
                className='w-[33%] bg-gray-900 text-white px-6 py-3 rounded-lg text-sm font-medium hover:bg-gray-800 transition-colors'
              >
                Sync Siteflow Inventory
              </button>

              <button
                onClick={() => handleUpload('Infigo', target)}
                className='w-[33%] bg-gray-900 text-white px-6 py-3 rounded-lg text-sm font-medium hover:bg-gray-800 transition-colors'
              >
                Sync Infigo Inventory
              </button>

              <button
                onClick={() => handleUpload('Both', target)}
                className='w-[33%] bg-gray-900 text-white px-6 py-3 rounded-lg text-sm font-medium hover:bg-gray-800 transition-colors'
              >
                Sync Both Inventory
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Right dashboard */}
      <div className='flex-1 flex flex-col w-[65%]'>
        {/* Header with tabs */}
        <div className='bg-white border-b border-gray-200 px-8 pt-8'>
          <h2 className='text-xl font-medium text-gray-900 mb-6'>
            Inventory Table
          </h2>

          <div className='flex gap-8'>
            <button
              onClick={() => setActiveTab('Infigo')}
              className={`pb-3 text-sm font-medium border-b-2 transition-colors ${
                activeTab === 'Infigo'
                  ? 'border-gray-900 text-gray-900'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              Infigo
            </button>
            <button
              onClick={() => setActiveTab('Siteflow')}
              className={`pb-3 text-sm font-medium border-b-2 transition-colors ${
                activeTab === 'Siteflow'
                  ? 'border-gray-900 text-gray-900'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              Siteflow
            </button>
          </div>
        </div>

        {/* Table */}
        <div className='flex-1 overflow-auto p-8'>
          <div className='grid grid-cols-2 gap-8'>
            {/* Successful SKUs Table */}
            <div className='bg-white rounded-lg border border-gray-200'>
              <h3 className='px-6 py-4 text-l font-semibold text-gray-700 uppercase'>
                Successful SKUs
              </h3>
              <table className='w-full'>
                <thead>
                  <tr className='border-b border-gray-200'>
                    <th className='text-left px-6 py-2 text-m font-medium text-green-600 uppercase tracking-wider'>
                      SKU
                    </th>
                    <th>{successCount}</th>
                  </tr>
                </thead>
                <tbody className='divide-y divide-gray-200'>
                  {successful.map((item, i) => (
                    <tr key={i} className='hover:bg-gray-50 transition-colors'>
                      <td className='px-6 py-4 text-m text-black-600'>
                        {item.sku}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Unsuccessful SKUs Table */}
            <div className='bg-white rounded-lg border border-gray-200'>
              <h3 className='px-6 py-4 text-l font-semibold text-gray-700 uppercase'>
                Unsuccessful SKUs
              </h3>
              <table className='w-full'>
                <thead>
                  <tr className='border-b border-gray-200'>
                    <th className='text-left px-6 py-2 text-m font-medium text-red-500 uppercase tracking-wider'>
                      SKU
                    </th>
                    <th>{failCount}</th>
                  </tr>
                </thead>
                <tbody className='divide-y divide-gray-200'>
                  {unsuccessful.map((item, i) => (
                    <tr key={i} className='hover:bg-gray-50 transition-colors'>
                      <td className='px-6 py-4 text-m text-gray-600'>
                        {item.sku}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default App;
