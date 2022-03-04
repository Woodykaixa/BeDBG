#include "asm.h"
using namespace BeDbgApi;

Type::handle_t Asm::CreateDecoder(ZydisMachineMode machineMode, ZydisAddressWidth addressWidth)
{
    auto* decoder = new ZydisDecoder;
    const auto status = ZydisDecoderInit(decoder, machineMode, addressWidth);
    if (ZYAN_SUCCESS(status))
    {
        return decoder;
    }
    delete decoder;
    return nullptr;
}

void Asm::DestroyDecoder(Type::handle_t decoder)
{
    delete static_cast<ZydisDecoder*>(decoder);
}