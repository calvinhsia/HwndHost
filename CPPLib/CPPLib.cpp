#include <windows.h>
#import "..\AreaFill\bin\debug\AreaFill.tlb" no_namespace
#include "atlbase.h"
#include "atlcom.h"
#include <queue>
#include <stack>
#include <functional>

#include <initguid.h>


// {BB4B9EE1-81DE-400B-A58A-687ED53A02E6}
DEFINE_GUID(CLSID_AreaFillCPP,
	0xbb4b9ee1, 0x81de, 0x400b, 0xa5, 0x8a, 0x68, 0x7e, 0xd5, 0x3a, 0x2, 0xe6);
using namespace std;

class MyAreaFill :
	public IAreaFill,
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<MyAreaFill, &CLSID_AreaFillCPP>
{
public:
	BEGIN_COM_MAP(MyAreaFill)
		COM_INTERFACE_ENTRY_IID(CLSID_AreaFillCPP, MyAreaFill)
		COM_INTERFACE_ENTRY(IAreaFill)
	END_COM_MAP()
	DECLARE_NOT_AGGREGATABLE(MyAreaFill)
	DECLARE_NO_REGISTRY()
	HRESULT __stdcall raw_DoAreaFill(
		AreaFillData areaFillData,
		AreaFillStats* pstats,
		long* pIsCancellationRequested,
		BYTE* array)
	{
		_areaFillData = areaFillData;
		_pstats = pstats;
		_pIsCancellationRequested = pIsCancellationRequested;
		_hdc = GetDC((HWND)areaFillData.hWnd);
		_cells = array;
		if (areaFillData.DepthFirst == VARIANT_FALSE)
		{
			queue<Point> queue;
			queue.push(areaFillData.StartPoint);
			DoTheFilling([&]() {return queue.size(); }, [&]() {auto pt = queue.front(); queue.pop(); return pt; }, [&](Point pt) {queue.push(pt); });
		}
		else
		{
			stack<Point> stack;
			stack.push(areaFillData.StartPoint);
			DoTheFilling([&]() {return stack.size(); }, [&]() {auto pt = stack.top(); stack.pop(); return pt; }, [&](Point pt) {stack.push(pt); });
		}
		ReleaseDC((HWND)areaFillData.hWnd, _hdc);
		return S_OK;
	}
private:
	AreaFillData _areaFillData;
	AreaFillStats* _pstats;
	long* _pIsCancellationRequested;
	HDC _hdc;
	RECT _rect;
	BYTE* _cells;
	COLORREF color = 0xffffff;

	void DoTheFilling(function<int()> getCount, function<Point()> getNextPoint, function<void(Point)> AddPoint)
	{
		while (getCount() > 0)
		{
			if (*_pIsCancellationRequested != 0)
			{
				break;
			}
			Point pt = getNextPoint();
			if (DrawCell(pt))
			{
				pt.X--;
				AddPoint(pt);
				pt.X += 2;
				AddPoint(pt);
				pt.X--; pt.Y++;
				AddPoint(pt);
				pt.Y -= 2;
				AddPoint(pt);
			}
		}
	}

	bool DrawCell(Point pt)
	{
		auto didDraw = false;
		if (pt.X >= 0 && pt.X < _areaFillData.ArraySize.X && pt.Y >= 0 && pt.Y < _areaFillData.ArraySize.Y)
		{
			_pstats->nPtsVisited++;
			auto ndx = pt.X * _areaFillData.ArraySize.Y + pt.Y;
			if (_cells[ndx] == 0)
			{
				_pstats->nPtsDrawn++;
				didDraw = true;
				_cells[ndx] = 1;
				color = (color + _areaFillData.ColorInc) & 0xffffff;
				SetPixel(_hdc, pt.X, pt.Y, color);
			}
		}
		return didDraw;
	}
};

OBJECT_ENTRY_AUTO(CLSID_AreaFillCPP, MyAreaFill)

// define a class that represents this module
class CAreaFillModule : public ATL::CAtlDllModuleT< CAreaFillModule >
{
#if _DEBUG
public:
	CAreaFillModule()
	{
		int x = 0; // set a bpt here
	}
	~CAreaFillModule()
	{
		int x = 0; // set a bpt here
	}
#endif _DEBUG
};


// instantiate a static instance of this class on module load
CAreaFillModule _AtlModule;
// this gets called by CLR due to env var settings
_Check_return_
STDAPI DllGetClassObject(__in REFCLSID rclsid, __in REFIID riid, __deref_out LPVOID FAR* ppv)
{
	HRESULT hr = E_FAIL;
	hr = AtlComModuleGetClassObject(&_AtlComModule, rclsid, riid, ppv);
	//  hr= CComModule::GetClassObject();
	return hr;
}
//tell the linker to export the function
#pragma comment(linker, "/EXPORT:DllGetClassObject=_DllGetClassObject@12,PRIVATE")

__control_entrypoint(DllExport)
STDAPI DllCanUnloadNow()
{
	return S_OK;
}
//tell the linker to export the function
#pragma comment(linker, "/EXPORT:DllCanUnloadNow=_DllCanUnloadNow@0,PRIVATE")


//
BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

