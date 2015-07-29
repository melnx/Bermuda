//==================================================================================================
///  @file Bermuda.CLIDSI.cpp
///
///  Implementation of the CLIDSI::LoadDriver().
///
//==================================================================================================

#include "CLIDSI.h"
#include "SimbaSettingReader.h"

//==================================================================================================
/// @brief Creates an instance of IDriver for a driver. 
///
/// @return IDriver instance. (OWN)
//==================================================================================================
Simba::DotNetDSI::IDriver^ Simba::CLIDSI::LoadDriver()
{
    // Set the driver branding.	
    SimbaSettingReader::SetConfigurationBranding("Simba\\BermudaODBCDSII");

    // ODBC Driver TODO #1: Construct driver singleton.
    return gcnew Bermuda::ODBC::Driver::BDriver();
}
