import { useState } from 'react';
import * as XLSX from 'xlsx';
import Papa from 'papaparse';
import { BASE_URL } from '../src/constant';

function App() {
  const [file, setFile] = useState<File | null>(null);

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
          syncInventory(results.data);
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
        syncInventory(jsonData);
      };
      reader.readAsArrayBuffer(file);
    } else {
      alert('Unsupported file type. Please upload CSV or Excel.');
    }
  };

  const syncInventory = async (jsonData: unknown[]) => {
    console.log('Inside Sync calling endpoint');
    try {
      const res = await fetch(`${BASE_URL}/api/siteflow/sync`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(jsonData),
      });

      const data = await res.json();
      console.log('Backend response:', data);
    } catch (err) {
      console.error('Error sending data to backend:', err);
    }
  };

  return (
    <div className='flex flex-col items-center justify-center h-screen gap-4'>
      <h1 className='text-2xl font-bold'>File Upload</h1>

      <input
        type='file'
        onChange={(e) => {
          const selectedFile = e.target.files?.[0] ?? null;
          setFile(selectedFile);
        }}
        className='border p-2 rounded'
      />

      <button
        onClick={handleUpload}
        className='bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600'
      >
        Upload
      </button>
    </div>
  );
}

export default App;
