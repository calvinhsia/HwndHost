#include <windows.h>
#import "..\AreaFill\bin\debug\AreaFill.tlb" no_namespace
#include "atlbase.h"
#include "atlcom.h"


#include <initguid.h>


// {BB4B9EE1-81DE-400B-A58A-687ED53A02E6}
DEFINE_GUID(CLSID_AreaFillCPP ,
    0xbb4b9ee1, 0x81de, 0x400b, 0xa5, 0x8a, 0x68, 0x7e, 0xd5, 0x3a, 0x2, 0xe6);

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
        long hWnd,
        struct Point ArraySize,
        struct Point StartPoint,
        VARIANT_BOOL DepthFirst,
        BYTE* array)
    {
        return S_OK;
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

