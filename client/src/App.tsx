import { useState } from 'react';
import * as XLSX from 'xlsx';
import Papa from 'papaparse';
import { BASE_URL } from '../src/constant';

type Item = {
  sku: string;
};

function App() {
  const [file, setFile] = useState<File | null>(null);
  const [activeTab, setActiveTab] = useState<'successful' | 'unsuccessful'>(
    'successful',
  );

  const [successfulSiteflow, setSuccessfulSiteflow] = useState<Item[]>([]);
  const [unsuccessfulSiteflow, setUnsuccessfulSiteflow] = useState<Item[]>([]);

  // Example data for test
  // const successfulItems = [
  //   { item: 'Example Item 1', sku: 'SKU123', quantity: 10 },
  //   { item: 'Example Item 2', sku: 'SKU124', quantity: 5 },
  // ];

  // const unsuccessfulItems = [
  //   { item: 'Bad Item 1', sku: 'SKU125', quantity: 0 },
  //   { item: 'Bad Item 2', sku: 'SKU126', quantity: 0 },
  // ];

  const handleUpload = () => {
    if (!file) {
      alert('Please select a file!');
      return;
    }

    const fileName = file.name.toLowerCase();

    if (fileName.endsWith('.csv')) {
      // Parse CSV
      Papa.parse(file, {
        header: true, // treat first row as headers
        complete: (results) => {
          console.log('CSV JSON data:', results.data);
          siteflowSync(results.data);
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
        siteflowSync(jsonData);
      };
      reader.readAsArrayBuffer(file);
    } else {
      alert('Unsupported file type. Please upload CSV or Excel.');
    }
  };

  const siteflowSync = async (jsonData: unknown[]) => {
    console.log('Inside Siteflow Sync calling endpoint');
    try {
      const res = await fetch(`${BASE_URL}/api/siteflow/sync`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(jsonData),
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

      console.log(successfulSiteflow);
    } catch (err) {
      console.error('Error sending data to backend:', err);
    }
  };

  const displayedItems =
    activeTab === 'successful' ? successfulSiteflow : unsuccessfulSiteflow;

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

            <div className='flex gap-4'>
              <button
                onClick={handleUpload}
                className='w-[33%] bg-gray-900 text-white px-6 py-3 rounded-lg text-sm font-medium hover:bg-gray-800 transition-colors'
              >
                Sync Siteflow Inventory
              </button>
              <button
                onClick={handleUpload}
                className='w-[33%] bg-gray-900 text-white px-6 py-3 rounded-lg text-sm font-medium hover:bg-gray-800 transition-colors'
              >
                Sync Infigo Inventory
              </button>
              <button
                onClick={handleUpload}
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
              onClick={() => setActiveTab('successful')}
              className={`pb-3 text-sm font-medium border-b-2 transition-colors ${
                activeTab === 'successful'
                  ? 'border-gray-900 text-gray-900'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              Successful
            </button>
            <button
              onClick={() => setActiveTab('unsuccessful')}
              className={`pb-3 text-sm font-medium border-b-2 transition-colors ${
                activeTab === 'unsuccessful'
                  ? 'border-gray-900 text-gray-900'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              Unsuccessful
            </button>
          </div>
        </div>

        {/* Table */}
        <div className='flex-1 overflow-auto p-8'>
          <div className='bg-white rounded-lg border border-black-200'>
            <table className='w-full'>
              <thead>
                <tr className='border-b border-black-200'>
                  <th className='text-left px-6 py-4 text-xs font-medium text-black-500 uppercase tracking-wider'>
                    SKU
                  </th>
                </tr>
              </thead>
              <tbody className='divide-y divide-blac-200'>
                {displayedItems.map((item, i) => (
                  <tr key={i} className='hover:bg-gray-50 transition-colors'>
                    <td className='px-6 py-4 text-sm text-black-600'>
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
  );
}

export default App;
