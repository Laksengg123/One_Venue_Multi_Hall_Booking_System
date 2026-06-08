using System;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Features.Halls;

namespace VenueBookingSystem.Features.Halls.Mapping
{
    public static class HallMapper
    {
        public static Hall FromReader(SqlDataReader r)
        {
            return new Hall(
                hallId:       r.GetInt32(r.GetOrdinal("HallId")),
                hallName:     r.GetString(r.GetOrdinal("HallName")),
                hallType:     r.GetString(r.GetOrdinal("HallType")),
                capacity:     r.GetInt32(r.GetOrdinal("Capacity")),
                pricePerHour: r.GetDecimal(r.GetOrdinal("PricePerHour")),
                description:  r.IsDBNull(r.GetOrdinal("Description")) ? string.Empty : r.GetString(r.GetOrdinal("Description")),
                location:     r.GetString(r.GetOrdinal("Location")),
                hasAC:        r.GetBoolean(r.GetOrdinal("HasAC")),
                hasProjector: r.GetBoolean(r.GetOrdinal("HasProjector")),
                hasWifi:      r.GetBoolean(r.GetOrdinal("HasWifi")),
                isActive:     r.GetBoolean(r.GetOrdinal("IsActive")),
                createdAt:    r.GetDateTime(r.GetOrdinal("CreatedAt"))
            );
        }
    }
}
