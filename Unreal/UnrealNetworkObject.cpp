// Fill out your copyright notice in the Description page of Project Settings.

#include "UnrealNetworkObject.h"
#include "NetManager.h"

int32 UUnrealNetworkObject::lastLocalID = 0;
// Sets default values for this component's properties
UUnrealNetworkObject::UUnrealNetworkObject()
{
	// Set this component to be initialized when the game starts, and to be ticked every frame.  You can turn these features
	// off to improve performance if you don't need them.

	PrimaryComponentTick.bCanEverTick = true;
	isLocallyOwned = false;
	localID = 0;
    globalID = 0;
}



// Called when the game starts
void UUnrealNetworkObject::BeginPlay()
{
	Super::BeginPlay();

    // Check if the object is locally owned
    if (isLocallyOwned)
    {
        localID = lastLocalID++;
        ANetManager::localNetObjects.Add(this);
    }

    ANetManager::netManagerInstance->AddNetworkObject(this);
}


// Called every frame
void UUnrealNetworkObject::TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

}

FString UUnrealNetworkObject::ToPacket()
{
    // Get the actor the component is attached to
    AActor* owner = GetOwner();

    // Get the actor's location and rotation
    FVector actorPosition = owner->GetActorLocation();
    FQuat actorRotation = (FQuat)owner->GetActorRotation();

    // Build a string in the right format for sending object data
    FString packetData = FString::Printf(TEXT("PositionRotationPacket,%i,%f,%f,%f,%f,%f,%f,%f"),
        globalID,  // Add global ID
        actorPosition.X, actorPosition.Y, actorPosition.Z,  // Add position
        actorRotation.X, actorRotation.Y, actorRotation.Z, actorRotation.W);  // Add rotation

    //packetData += FString::Printf(TEXT("PositionRotationPacket"), globalID);  // Add global ID
    //packetData += VectorToString(actorPosition) + TEXT(",");  // Add position
    //packetData += RotatorToString(actorRotation);  // Add rotation

    return packetData;
}

void UUnrealNetworkObject::FromPacket(FString packetData)
{
    // Create a variable to hold the broken down packet string
    //TArray<FString> packetParts;
    //const TCHAR* delims[] = { TEXT(":"), TEXT(";") };
    //packetData.ParseIntoArray(packetParts, delims, 2);

    //// Break down the big string into smaller chunks of usable data
    //if (packetParts.Num() == 8) {
    //    // Parse packet data
    //    globalID = FCString::Atoi(*packetParts[0]);
    //    FVector actorLocation(FCString::Atof(*packetParts[2]), FCString::Atof(*packetParts[3]), FCString::Atof(*packetParts[4]));
    //    FQuat actorRotation(FCString::Atof(*packetParts[5]), FCString::Atof(*packetParts[6]), FCString::Atof(*packetParts[7]), FCString::Atof(*packetParts[8]));


    //    // Set the object's position and rotation
    //    AActor* owner = GetOwner();
    //    
    //    owner->SetActorLocation(actorLocation);
    //    owner->SetActorRotation(actorRotation);
    //    
    //}

    // Create a variable to hold the broken down packet string
    TArray<FString> packetParts;
    const TCHAR* delims[] = { TEXT(",") }; // , TEXT(";")

    packetData.ParseIntoArray(packetParts, delims, 1);

    if (packetParts[0].Contains("PositionRotationPacket"))
    {
        float posX = FCString::Atof(*packetParts[2]);
        float posY = FCString::Atof(*packetParts[3]);
        float posZ = FCString::Atof(*packetParts[4]);
        float rotX = FCString::Atof(*packetParts[5]);
        float rotY = FCString::Atof(*packetParts[6]);
        float rotZ = FCString::Atof(*packetParts[7]);

        FVector actorPosition = FVector(posX, posY, posZ);
        FQuat actorRotation = FQuat::MakeFromEuler(FVector(rotX, rotY, rotZ));

        AActor* owner = GetOwner();

        owner->SetActorLocation(actorPosition);
        owner->SetActorRotation(actorRotation);
    }

    else {
        UE_LOG(LogTemp, Error, TEXT("Invalid packet format"));
    }
}
//
//FString UUnrealNetworkObject::VectorToString(const FVector& vector) const
//{
//    return FString::Printf(TEXT("%f|%f|%f"), vector.X, vector.Y, vector.Z);
//}
//
//FVector UUnrealNetworkObject::StringToVector(const FString& vectorString) const
//{
//    TArray<FString> parts;
//    vectorString.ParseIntoArray(parts, TEXT("|"));
//
//    if (parts.Num() == 3) {
//        return FVector(FCString::Atof(*parts[0]), FCString::Atof(*parts[1]), FCString::Atof(*parts[2]));
//    }
//    return FVector::ZeroVector;
//}
//
//FString UUnrealNetworkObject::RotatorToString(const FRotator& rotator) const
//{
//    return FString();
//}
//
//FRotator UUnrealNetworkObject::StringToRotator(const FString& rotatorString) const
//{
//    TArray<FString> parts;
//    rotatorString.ParseIntoArray(parts, TEXT("|"));
//    if (parts.Num() == 3)
//    {
//        return FRotator(FCString::Atof(*parts[0]), FCString::Atof(*parts[1]), FCString::Atof(*parts[2]));
//    }
//    return FRotator::ZeroRotator;
//}

