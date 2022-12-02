#include <windows.h>
#import "..\AreaFill\bin\debug\AreaFill.tlb" no_namespace
#include "atlbase.h"
#include "atlcom.h"
//#define _ITERATOR_DEBUG_LEVEL 0
#include <queue>
#include <stack>
#include <functional>

#include <initguid.h>
#ifndef _DEBUG
//	#include "Release\areafill.tlh" // why do we need this sometimes to suppress squiggle errors? 17.5 Preview 2 33129.541.main
#endif

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
		long* pColor,
		long* pIsCancellationRequested,
		BYTE* array)
	{
		_areaFillData = areaFillData;
		_pstats = pstats;
		_pColor = pColor;
		_pIsCancellationRequested = pIsCancellationRequested;
		_hdc = GetDC((HWND)areaFillData.hWnd);
		_cells = array;
		if (areaFillData.DepthFirst == VARIANT_FALSE)
		{
			queue<Point> queue;
			queue.push(areaFillData.StartPoint);
			DoTheFilling([&] {return queue.size(); }, [&] {auto pt = queue.front(); queue.pop(); return pt; }, [&](Point pt) {queue.push(pt); });
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
	long* _pColor;
	long* _pIsCancellationRequested;
	HDC _hdc;
	RECT _rect;
	BYTE* _cells;
#define Filled 1
#define NDXFUNC(pt) (pt.X * _areaFillData.ArraySize.Y + ptCurrent.Y)
	void DoTheFilling(function<int()> getCount, function<Point()> getNextPoint, function<void(Point)> AddPoint)
	{
		while (true)
		{
			auto nCnt = getCount();
			if (nCnt == 0 || *_pIsCancellationRequested != 0)
			{
				break;
			}
			if (nCnt > _pstats->nMaxDepth)
			{
				_pstats->nMaxDepth = nCnt;
			}
			Point ptCurrent = getNextPoint();
			_pstats->nPtsVisited++;
			if (ptCurrent.X >= 0 && ptCurrent.X < _areaFillData.ArraySize.X && ptCurrent.Y >= 0 && ptCurrent.Y < _areaFillData.ArraySize.Y)
			{
				if (_cells[NDXFUNC(ptCurrent)] == 0)
				{
					auto DidDrawCurrent = false;
					if (_areaFillData.FillViaPixels == VARIANT_FALSE)
					{
						auto ptWestBound = ptCurrent;
						ptWestBound.X--;
						while (_cells[NDXFUNC(ptWestBound)] != Filled)
						{
							ptWestBound.X--;
						}
						ptWestBound.X++;
						auto ptEastBound = ptCurrent;
						ptEastBound.X++;
						while (_cells[NDXFUNC(ptEastBound)] != Filled)
						{
							ptEastBound.X++;
						}
						ptEastBound.X--;
						if (ptWestBound.X != ptEastBound.X)
						{
							DrawLineRaw(ptWestBound, ptEastBound);
							_pstats->nPtsDrawn += ptEastBound.X - ptWestBound.X;
							for (; ptWestBound.X <= ptEastBound.X; ptWestBound.X++)
							{
								_cells[NDXFUNC(ptWestBound)] = Filled;
								ptWestBound.Y++;
								AddPoint(ptWestBound);
								ptWestBound.Y -= 2;
								AddPoint(ptWestBound);
								ptWestBound.Y++;
							}
						}
						else
						{
							_pstats->nPtsDrawn++;
							DrawCell(ptCurrent);
							AddNESW(ptCurrent, AddPoint);
							DidDrawCurrent = true;
						}
						auto ptNorthBound = ptCurrent;
						ptNorthBound.Y--;
						while (_cells[NDXFUNC(ptNorthBound)] != Filled)
						{
							ptNorthBound.Y--;
						}
						ptNorthBound.Y++;
						auto ptSouthBound = ptCurrent;
						ptSouthBound.Y++;
						while (_cells[NDXFUNC(ptSouthBound)] != Filled)
						{
							ptSouthBound.Y++;
						}
						ptSouthBound.Y--;
						if (ptSouthBound.Y != ptNorthBound.Y)
						{
							DrawLineRaw(ptNorthBound, ptSouthBound);
							_pstats->nPtsDrawn += ptSouthBound.Y - ptNorthBound.Y;
							for (; ptNorthBound.Y <= ptSouthBound.Y; ptNorthBound.Y++)
							{
								_cells[NDXFUNC(ptNorthBound)] = Filled;
								ptNorthBound.X--;
								AddPoint(ptNorthBound);
								ptNorthBound.X += 2;
								AddPoint(ptNorthBound);
								ptNorthBound.X--;
							}
						}
						else
						{
							if (!DidDrawCurrent)
							{
								_pstats->nPtsDrawn++;
								DrawCell(ptCurrent);
								AddNESW(ptCurrent, AddPoint);
							}
						}
					}
					else
					{
						if (DrawCell(ptCurrent))
						{
							AddNESW(ptCurrent, AddPoint);
						}
					}
				}

			}

		}
	}
	void AddNESW(Point pt, function<void(Point)> AddPoint)
	{
		pt.X--;
		AddPoint(pt);
		pt.X += 2;
		AddPoint(pt);
		pt.X--;
		pt.Y++;
		AddPoint(pt);
		pt.Y -= 2;
		AddPoint(pt);
	}
	void DrawLineRaw(Point pt0, Point pt1)
	{
		auto pen = CreatePen(0, 1, *_pColor);
		*_pColor = (*_pColor + _areaFillData.ColorInc) & 0xffffff;
		SelectObject(_hdc, pen);
		MoveToEx(_hdc, pt0.X, pt0.Y, nullptr);
		LineTo(_hdc, pt1.X, pt1.Y);
		DeleteObject(pen);
	}
	bool DrawCell(Point pt)
	{
		auto didDraw = false;
		if (pt.X >= 0 && pt.X < _areaFillData.ArraySize.X && pt.Y >= 0 && pt.Y < _areaFillData.ArraySize.Y)
		{
			auto ndx = pt.X * _areaFillData.ArraySize.Y + pt.Y;
			if (_cells[ndx] == 0)
			{
				_pstats->nPtsDrawn++;
				didDraw = true;
				_cells[ndx] = 1;
				*_pColor = (*_pColor + _areaFillData.ColorInc) & 0xffffff;
				SetPixel(_hdc, pt.X, pt.Y, *_pColor);
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

