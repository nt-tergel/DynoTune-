namespace DynoTune.Services;

public class AmdAdlxService
{
    private ADLXHelper? _helper;

    public bool Initialize()
    {
        if (_helper is not null)
        {
            return true;
        }

        // Sample pattern: create ADLXHelper, then call Initialize.
        _helper = new ADLXHelper();
        ADLX_RESULT result = _helper.Initialize();
        if (result == ADLX_RESULT.ADLX_OK)
        {
            return true;
        }

        _helper = null;
        return false;
    }

    public void Shutdown()
    {
        if (_helper is null)
        {
            return;
        }

        // Sample pattern: terminate through the same helper instance.
        _helper.Terminate();
        _helper = null;
    }
}