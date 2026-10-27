<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="text" indent="no"/>
    <!-- Recursive template to pad a string to the right -->
    <xsl:template name="PadRight">
        <xsl:param name="value" />
        <xsl:param name="length" />
        <xsl:param name="currentLength" select="string-length($value)" />

        <xsl:choose>
            <!-- If the current length is less than the desired length, add a space -->
            <xsl:when test="$currentLength &lt; $length">
                <xsl:call-template name="PadRight">
                    <xsl:with-param name="value" select="concat($value, ' ')" />
                    <xsl:with-param name="length" select="$length" />
                    <xsl:with-param name="currentLength" select="string-length(concat($value, ' '))" />
                </xsl:call-template>
            </xsl:when>
            <!-- If the desired length is reached, output the value -->
            <xsl:otherwise>
                <xsl:value-of select="$value" />
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    <xsl:template match="/">
        <Transform>
            <PatientName>
                <xsl:call-template name="PadRight">
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test='eob_xml/eob/VR003/record/patient_name and contains(eob_xml/eob/VR003/record/patient_name, ",")'>
                                <xsl:variable name="patientName" select="eob_xml/eob/VR003/record/patient_name" />
                                <xsl:value-of select="concat(substring-after($patientName, ','), ' ', substring-before($patientName, ','))"/>
                            </xsl:when>
                            <xsl:when test='eob_xml/eob/VR003/record/patient_name'>
                                <xsl:value-of select='eob_xml/eob/VR003/record/patient_name'/>
                            </xsl:when>
                            <xsl:when test="eob_xml/eob/VR003/record/patient_first_name and eob_xml/eob/VR003/record/patient_last_name">
                                <xsl:value-of select="concat(eob_xml/eob/VR003/record/patient_first_name, ' ', eob_xml/eob/VR003/record/patient_last_name)"/>
                            </xsl:when>
                            <xsl:when test='eob_xml/eob_data/eob/patient_name and contains(eob_xml/eob_data/eob/patient_name, ",")'>
                                <xsl:variable name="patientName" select="eob_xml/eob_data/eob/patient_name" />
                                <xsl:value-of select="concat(substring-after($patientName, ','), ' ', substring-before($patientName, ','))"/>
                            </xsl:when>
                            <xsl:when test="eob_xml/eob_data/eob/patient_name">
                                <xsl:value-of select='eob_xml/eob_data/eob/patient_name'/>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:with-param>
                    <xsl:with-param name="length" select="20" />
                </xsl:call-template>
            </PatientName>
            <PatientDOB>
                <xsl:call-template name="PadRight">
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test='eob_xml/eob/VR003/record/date_of_birth'>
                                <xsl:value-of select='eob_xml/eob/VR003/record/date_of_birth'/>
                            </xsl:when>
                            <xsl:when test='eob_xml/eob/VR003/record/patient_dob'>
                                <xsl:value-of select='eob_xml/eob/VR003/record/patient_dob'/>
                            </xsl:when>
                            <xsl:when test='eob_xml/eob_data/eob/PatientBirthDate'>
                                <xsl:value-of select='eob_xml/eob_data/eob/PatientBirthDate'/>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:with-param>
                    <xsl:with-param name="length" select="20" />
                </xsl:call-template>
            </PatientDOB>
            <ClaimId>
                <xsl:call-template name="PadRight">
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test='eob_xml/eob/VR003/record/claim_number'>
                                <xsl:value-of select='eob_xml/eob/VR003/record/claim_number'/>
                            </xsl:when>
                            <xsl:when test ='eob_xml/eob_data/eob/ClaimNumber'>
                                <xsl:value-of select='eob_xml/eob_data/eob/ClaimNumber'/>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:with-param>
                    <xsl:with-param name="length" select="20" />
                </xsl:call-template>
            </ClaimId>
            <PatientAccountNumber>
                <xsl:call-template name="PadRight">
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test='eob_xml/eob/VR003/record/patient_account_number'>
                                <xsl:value-of select='eob_xml/eob/VR003/record/patient_account_number'/>
                            </xsl:when>
                            <xsl:when test='eob_xml/eob_data/eob/PatientAccountNumber'>
                                <xsl:value-of select='eob_xml/eob_data/eob/PatientAccountNumber'/>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:with-param>
                    <xsl:with-param name="length" select="20" />
                </xsl:call-template>
            </PatientAccountNumber>
            <Amount>
                <xsl:call-template name="PadRight">
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test='eob_xml/eob/load_amount'>
                                <xsl:value-of select='eob_xml/eob/load_amount'/>
                            </xsl:when>
                            <xsl:when test='eob_xml/eob_data/eob/Amount'>
                                <xsl:value-of select='eob_xml/eob_data/eob/Amount'/>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:with-param>
                    <xsl:with-param name="length" select="20" />
                </xsl:call-template>
            </Amount>
            <ClaimDate>
                <xsl:call-template name="PadRight">
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test='eob_xml/eob/VR003/record/date_of_service'>
                                <xsl:value-of select='eob_xml/eob/VR003/record/date_of_service'/>
                            </xsl:when>
                            <xsl:when test='eob_xml/eob/VR003/record/date_of_service_from'>
                                <xsl:value-of select='eob_xml/eob/VR003/record/date_of_service_from'/>
                            </xsl:when>
                            <xsl:when test='eob_xml/eob_data/eob/DateOfService'>
                                <xsl:value-of select='eob_xml/eob_data/eob/DateOfService'/>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:with-param>
                    <xsl:with-param name="length" select="10" />
                </xsl:call-template>
            </ClaimDate>
            <SubscriberName>
                <xsl:call-template name="PadRight">
                    <xsl:with-param name="value">
                        <xsl:choose>
                             <xsl:when test="eob_xml/eob/VR003/record/subscriber_first_name and eob_xml/eob/VR003/record/subscriber_last_name">
                                <xsl:value-of select="concat(eob_xml/eob/VR003/record/subscriber_first_name, ' ', eob_xml/eob/VR003/record/subscriber_last_name)"/>
                            </xsl:when>
                            <xsl:when test="eob_xml/eob_data/eob/member_name and contains(eob_xml/eob_data/eob/member_name, ',')">
                                <xsl:variable name="memberName" select="eob_xml/eob_data/eob/member_name" />
                                <xsl:value-of select="concat(substring-after($memberName, ','), ' ', substring-before($memberName, ','))"/>
                            </xsl:when>
                            <xsl:when test="eob_xml/eob/VR003/record/member_name and string-length(normalize-space(eob_xml/eob/VR003/record/member_name)) &gt; 0 and contains(eob_xml/eob/VR003/record/member_name, ',')">
                                <xsl:variable name="memberName" select="eob_xml/eob/VR003/record/member_name" />
                                <xsl:value-of select="concat(substring-after($memberName, ','), ' ', substring-before($memberName, ','))"/>
                            </xsl:when>
                            <xsl:when test="eob_xml/eob/VR003/record/member_name and string-length(normalize-space(eob_xml/eob/VR003/record/member_name)) &gt; 0">
                                <xsl:value-of select="eob_xml/eob/VR003/record/member_name"/>
                            </xsl:when>
                            <xsl:when test="eob_xml/eob_data/eob/member_name">
                                <xsl:value-of select="eob_xml/eob_data/eob/member_name"/>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:with-param>
                    <xsl:with-param name="length" select="30" />
                </xsl:call-template>
            </SubscriberName>
            <MainGovtIdNumber>
                <xsl:call-template name="PadRight">
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test="eob_xml/eob/main_govt_id_number">
                                <xsl:value-of select="eob_xml/eob/main_govt_id_number"/>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:with-param>
                    <xsl:with-param name="length" select="10" />
                </xsl:call-template>
            </MainGovtIdNumber>
            <PayeeName>
            <xsl:call-template name="PadRight">
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test="eob_xml/eob/payee_name">
                                <xsl:value-of select="eob_xml/eob/payee_name"/>
                            </xsl:when>
                            <xsl:when test="eob_xml/eob_data/eob/PayeeName">
                                <xsl:value-of select='eob_xml/eob_data/eob/PayeeName'/>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:with-param>
                    <xsl:with-param name="length" select="30" />
                </xsl:call-template>
            </PayeeName>
            <PayeeAddress1>
                <xsl:call-template name="PadRight">
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test='eob_xml/eob/payee_address_1'>
                                <xsl:value-of select='eob_xml/eob/payee_address_1'/>
                            </xsl:when>
                            <xsl:when test='eob_xml/eob_data/eob/PayeeAddr1'>
                                <xsl:value-of select='eob_xml/eob_data/eob/PayeeAddr1'/>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:with-param>
                    <xsl:with-param name="length" select="30" />
                </xsl:call-template>
            </PayeeAddress1>
            <PayeeCity>
                <xsl:call-template name="PadRight">
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test='eob_xml/eob/payee_city'>
                                <xsl:value-of select='eob_xml/eob/payee_city'/>
                            </xsl:when>
                            <xsl:when test='eob_xml/eob_data/eob/PayeeCity'>
                                <xsl:value-of select='eob_xml/eob_data/eob/PayeeCity'/>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:with-param>
                    <xsl:with-param name="length" select="30" />
                </xsl:call-template>
            </PayeeCity>
            <PayeeState>
                <xsl:call-template name="PadRight">
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test='eob_xml/eob/payee_state'>
                                <xsl:value-of select='eob_xml/eob/payee_state'/>
                            </xsl:when>
                            <xsl:when test='eob_xml/eob_data/eob/PayeeState'>
                                <xsl:value-of select='eob_xml/eob_data/eob/PayeeState'/>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:with-param>
                    <xsl:with-param name="length" select="20" />
                </xsl:call-template>
            </PayeeState>
            <PayeePostalCode>
                <xsl:call-template name="PadRight">
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test='eob_xml/eob/payee_postal_code'>
                                <xsl:value-of select='eob_xml/eob/payee_postal_code'/>
                            </xsl:when>
                            <xsl:when test='eob_xml/eob_data/eob/PayeePostalCode'>
                                <xsl:value-of select='eob_xml/eob_data/eob/PayeePostalCode'/>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:with-param>
                    <xsl:with-param name="length" select="20" />
                </xsl:call-template>
            </PayeePostalCode>
        </Transform>
    </xsl:template>
</xsl:stylesheet>