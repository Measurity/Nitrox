#if NET9_0_OR_GREATER
global using LockObject = System.Threading.Lock;
#else
global using LockObject = object;
#endif
global using NitroxModel.Extensions;
