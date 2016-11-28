﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using System.IO;

namespace IPhoneNotifications.AppleNotificationCenterService
{
    public class ControlPoint
    {
        public readonly GattCharacteristic GattCharacteristic;

        public ControlPoint(GattCharacteristic characteristic)
        {
            GattCharacteristic = characteristic;
        }

        public async void PerformNotificationActionAsync(UInt32 notificationUID, ActionID actionID)
        {
            GattCommunicationStatus status;

            //Relay notification action back to device
            NotificationActionData command = new NotificationActionData(CommandID.PerformNotificationAction, notificationUID, actionID);

            var bytes = command.ToArray();

            try
            {
                status = await GattCharacteristic.WriteValueAsync(bytes.AsBuffer(), GattWriteOption.WriteWithResponse);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// The Get App Attributes command allows an NC to retrieve attributes of a specific app installed on the NP.
        /// </summary>
        /// <param name="appIdentifier">The string identifier of the app the client wants information about.</param>
        /// <param name="attributeIDs">A list of attributes the NC wants to retrieve.</param>
        /// <returns></returns>
        public async Task<GattCommunicationStatus> GetAppAttributesAsync(string appIdentifier, List<AppAttributeID> attributeIDs)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write((byte)CommandID.GetNotificationAttributes);
            writer.Write(appIdentifier);
            writer.Write((byte)0); // App identifier must be a null terminated string

            foreach (NotificationAttributeID attrID in attributeIDs)
            {
                writer.Write((byte)attrID);
            }

            byte[] bytes = stream.ToArray();

            // Send the command
            try
            {
                if (GattCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.ReliableWrites))
                {
                    GattReliableWriteTransaction transaction = new GattReliableWriteTransaction();
                    transaction.WriteValue(GattCharacteristic, bytes.AsBuffer());
                    return await transaction.CommitAsync();
                }
                else
                {
                    return await GattCharacteristic.WriteValueAsync(bytes.AsBuffer(), GattWriteOption.WriteWithResponse);
                }
            }
            catch (Exception e)
            {
                switch ((uint)e.HResult)
                {
                    case 0xE04200A0:
                        System.Diagnostics.Debug.WriteLine("Unknown command. The commandID was not recognized by the NP.");
                        return GattCommunicationStatus.Success;
                    case 0xE04200A1:
                        System.Diagnostics.Debug.WriteLine("Invalid command. The command was improperly formatted.");
                        return GattCommunicationStatus.Success;
                    case 0xE04200A2:
                        System.Diagnostics.Debug.WriteLine("Invalid parameter. One of the parameters (for example, the NotificationUID) does not refer to an existing object on the NP.");
                        return GattCommunicationStatus.Success;
                    case 0xE04200A3:
                        System.Diagnostics.Debug.WriteLine("Action failed. The action was not performed.");
                        return GattCommunicationStatus.Success;
                    default:
                        System.Diagnostics.Debug.WriteLine("Failed to get notification attributes. " + e.Message);
                        break;
                }

                throw e;
            }
        }

        /// <summary>
        /// The Get Notification Attributes command allows an NC to retrieve the attributes of a specific iOS notification.
        /// </summary>
        /// <param name="notificationUID">The 32-bit numerical value representing the UID of the iOS notification for which the client wants information.</param>
        /// <param name="attributeIDs"></param>
        /// <returns></returns>
        public async Task<GattCommunicationStatus> GetNotificationAttributesAsync(UInt32 notificationUID, List<NotificationAttributeID> attributeIDs)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write((byte)CommandID.GetNotificationAttributes);
            writer.Write(notificationUID);

            foreach (NotificationAttributeID attrID in attributeIDs)
            {
                writer.Write((byte)attrID);

                // Some attributes need to be followed by a 2-bytes max length parameter
                switch (attrID)
                {
                    case NotificationAttributeID.Message:
                        // Max length for title
                        writer.Write((UInt16)128);
                        break;
                    case NotificationAttributeID.Subtitle:
                        // Max length for title
                        writer.Write((UInt16)64);
                        break;
                    case NotificationAttributeID.Title:
                        // Max length for title
                        writer.Write((UInt16)64);
                        break;
                }
            }

            byte[] bytes = stream.ToArray();

            // Send the command
            try
            {
                if (GattCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.ReliableWrites))
                {
                    GattReliableWriteTransaction transaction = new GattReliableWriteTransaction();
                    transaction.WriteValue(GattCharacteristic, bytes.AsBuffer());
                    return await transaction.CommitAsync();
                }
                else
                {
                    return await GattCharacteristic.WriteValueAsync(bytes.AsBuffer(), GattWriteOption.WriteWithResponse);
                }
            }
            catch (Exception e)
            {
                switch ((uint)e.HResult)
                {
                    case 0xE04200A0:
                        System.Diagnostics.Debug.WriteLine("Unknown command. The commandID was not recognized by the NP.");
                        return GattCommunicationStatus.Success;
                    case 0xE04200A1:
                        System.Diagnostics.Debug.WriteLine("Invalid command. The command was improperly formatted.");
                        return GattCommunicationStatus.Success;
                    case 0xE04200A2:
                        System.Diagnostics.Debug.WriteLine("Invalid parameter. One of the parameters (for example, the NotificationUID) does not refer to an existing object on the NP.");
                        return GattCommunicationStatus.Success;
                    case 0xE04200A3:
                        System.Diagnostics.Debug.WriteLine("Action failed. The action was not performed.");
                        return GattCommunicationStatus.Success;
                    default:
                        System.Diagnostics.Debug.WriteLine("Failed to get notification attributes. " + e.Message);
                        break;
                }

                throw e;
            }
        }
    }
}
